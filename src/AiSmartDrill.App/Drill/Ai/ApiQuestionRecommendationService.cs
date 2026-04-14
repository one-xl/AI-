using System.Text;
using System.Text.Json;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Drill.Ai.Ark;
using AiSmartDrill.App.Drill.Ai.Client;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于火山方舟的题目推荐：向模型发送领域、分类标签、关键词与错题上下文，解析其返回的
/// FocusTags/FocusKeywords 后在候选集中严格筛选，并排除错题本中的题目。
/// </summary>
public sealed class ApiQuestionRecommendationService : IQuestionRecommendationService
{
    private const int TargetCount = 8;
    private const int MaxCatalogLines = 400;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IChatCompletionService _chat;
    private readonly ILogger<ApiQuestionRecommendationService> _logger;
    private readonly AiCallTrace _trace;

    /// <summary>
    /// 初始化 <see cref="ApiQuestionRecommendationService"/>。
    /// </summary>
    public ApiQuestionRecommendationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IChatCompletionService chatCompletionService,
        ILogger<ApiQuestionRecommendationService> logger,
        AiCallTrace trace)
    {
        _dbFactory = dbFactory;
        _chat = chatCompletionService;
        _logger = logger;
        _trace = trace;
    }

    /// <inheritdoc />
    public async Task<QuestionRecommendationDto> RecommendAsync(
        long userId,
        QuestionRecommendationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        request ??= new QuestionRecommendationRequest();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var wrongQuestionIds = await (
                from w in db.WrongBookEntries.AsNoTracking()
                where w.UserId == userId
                join q in db.Questions.AsNoTracking() on w.QuestionId equals q.Id
                where request.DomainScope == null || q.Domain == request.DomainScope.Value
                select w.QuestionId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var wrongSet = wrongQuestionIds.ToHashSet();

        var contextQuestionIds = request.SelectedWrongQuestionIds.Count > 0
            ? request.SelectedWrongQuestionIds.Where(wrongSet.Contains).Distinct().ToList()
            : wrongQuestionIds;

        var performanceSummary = await BuildPerformanceSummaryAsync(db, userId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<Question> contextQuestions;
        if (contextQuestionIds.Count > 0)
        {
            contextQuestions = await db.Questions.AsNoTracking()
                .Where(q => contextQuestionIds.Contains(q.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            contextQuestions = Array.Empty<Question>();
        }

        var candidates = await (
                from q in db.Questions.AsNoTracking()
                where q.IsEnabled && !wrongSet.Contains(q.Id)
                where request.DomainScope == null || q.Domain == request.DomainScope.Value
                orderby q.Id
                select q)
            .Take(MaxCatalogLines)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidates.Count < TargetCount)
        {
            _logger.LogWarning("候选题不足 {Need}（当前 {Have}），使用本地推荐。", TargetCount, candidates.Count);
            return await FallbackLocalAsync(db, userId, request, wrongSet, candidates, contextQuestions, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            var dto = await CallArkAsync(
                    performanceSummary,
                    request,
                    candidates,
                    contextQuestions,
                    wrongQuestionIds,
                    cancellationToken)
                .ConfigureAwait(false);
            _trace.Set("recommend:ark", true);
            _logger.LogInformation("AI 题目推荐（Ark）：UserId={UserId}, Count={Count}", userId, dto.RecommendedQuestionIds.Count);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ark 题目推荐失败，回退本地");
            _trace.Set("recommend:local-fallback", false);
            return await FallbackLocalAsync(db, userId, request, wrongSet, candidates, contextQuestions, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task<UserPerformanceSummary> BuildPerformanceSummaryAsync(
        AppDbContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var wrongEntries = await db.WrongBookEntries.AsNoTracking()
            .Where(w => w.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var answerRecords = await db.AnswerRecords.AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new UserPerformanceSummary
        {
            UserId = userId,
            TotalAttempts = answerRecords.Count,
            CorrectAttempts = answerRecords.Count(a => a.IsCorrect),
            WrongBookCount = wrongEntries.Count,
            WeakTags = await GetWeakKnowledgeTagsAsync(db, userId, cancellationToken).ConfigureAwait(false)
        };
    }

    private static async Task<IReadOnlyList<string>> GetWeakKnowledgeTagsAsync(
        AppDbContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var wrongTags = await db.WrongBookEntries
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (_, q) => q)
            .Select(q => q.KnowledgeTags)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var tagCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var tags in wrongTags)
        {
            foreach (var t in RecommendationMatcher.Tokenize(tags))
            {
                tagCount[t] = tagCount.TryGetValue(t, out var c) ? c + 1 : 1;
            }
        }

        return tagCount
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => kv.Key)
            .ToList();
    }

    private async Task<QuestionRecommendationDto> CallArkAsync(
        UserPerformanceSummary summary,
        QuestionRecommendationRequest request,
        IReadOnlyList<Question> candidates,
        IReadOnlyList<Question> contextQuestions,
        IReadOnlyList<long> wrongQuestionIds,
        CancellationToken cancellationToken)
    {
        var weakTagsString = string.Join(", ", summary.WeakTags);
        var domainNote = request.DomainScope is { } dom
            ? $"推荐领域限定为：{dom}（候选表仅含该领域且已排除错题本中该领域下的题目）。"
            : "推荐领域：不限（候选表已排除用户错题本中的全部题目）。";

        var wrongLines = new StringBuilder();
        if (contextQuestions.Count > 0)
        {
            wrongLines.AppendLine("错题上下文（用于推断 FocusTags/FocusKeywords；勿推荐下列 Id）：");
            foreach (var q in contextQuestions.Take(24))
            {
                var stem = q.Stem.Length > 72 ? q.Stem[..72] + "…" : q.Stem;
                wrongLines.AppendLine(
                    $"Id={q.Id}; Domain={q.Domain}; TopicTags={q.TopicTags}; TopicKeywords={q.TopicKeywords}; KnowledgeTags={q.KnowledgeTags}; Stem={stem}");
            }
        }
        else
        {
            wrongLines.AppendLine("当前无错题条目或领域筛选后无错题，请结合弱项知识点与候选表自行给出 FocusTags/FocusKeywords。");
            if (wrongQuestionIds.Count > 0)
            {
                wrongLines.AppendLine("用户错题本题目 Id（勿推荐）：" + string.Join(", ", wrongQuestionIds.Take(40)));
            }
        }

        var catalog = new StringBuilder();
        catalog.AppendLine("候选题目表（每行：Id|Domain|TopicTags|TopicKeywords|KnowledgeTags|Stem前60字）：");
        foreach (var q in candidates)
        {
            var stem = q.Stem.Length > 60 ? q.Stem[..60] + "…" : q.Stem;
            catalog.AppendLine(
                $"{q.Id}|{q.Domain}|{q.TopicTags}|{q.TopicKeywords}|{q.KnowledgeTags}|{stem}");
        }

        var messages = new List<ChatMessage>
        {
            new()
            {
                Role = "system",
                Content =
                    "你是教育场景助手。只输出一个 JSON 对象，不要 Markdown，不要解释文字。\n" +
                    "字段：Rationale(string)、FocusTags(string[])、FocusKeywords(string[])、RecommendedQuestionIds(number[])。\n" +
                    "FocusTags 与 FocusKeywords 由你根据错题上下文与弱项推断，用于后续在题库中筛选：标签应贴近 TopicTags/KnowledgeTags 中的词汇；关键词应可命中题干或 TopicKeywords。\n" +
                    "RecommendedQuestionIds 须恰好 " + TargetCount + " 个，且每个 Id 必须出现在用户给出的候选表中；不得包含错题上下文中列出的 Id 或「勿推荐」列表中的 Id。\n" +
                    "若领域已限定，只推荐该 Domain 的题。"
            },
            new()
            {
                Role = "user",
                Content =
                    domainNote + "\n" +
                    $"学习摘要：总答题={summary.TotalAttempts}, 正确={summary.CorrectAttempts}, 错题条目={summary.WrongBookCount}, 弱项知识点={weakTagsString}\n\n" +
                    wrongLines + "\n" +
                    catalog
            }
        };

        var response = await _chat.GenerateCompletionAsync(messages, null, cancellationToken).ConfigureAwait(false);
        var raw = ArkAssistantReply.GetPrimaryText(response);
        var json = ArkModelOutputParsing.ExtractFirstJsonValue(raw);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("模型返回为空。");
        }

        var parsed = JsonSerializer.Deserialize<ArkRecommendationPayload>(json, ArkChatJsonDefaults.ModelPayloadOptions);
        if (parsed is null)
        {
            throw new InvalidOperationException("JSON 无效。");
        }

        var focusTags = RecommendationMatcher.NormalizeTokens(parsed.FocusTags);
        var focusKeywords = RecommendationMatcher.NormalizeTokens(parsed.FocusKeywords);
        var allowed = candidates.Select(q => q.Id).ToHashSet();

        var aiPicked = new List<long>();
        if (parsed.RecommendedQuestionIds is not null)
        {
            foreach (var id in parsed.RecommendedQuestionIds)
            {
                if (aiPicked.Count >= TargetCount)
                {
                    break;
                }

                if (allowed.Contains(id) && !aiPicked.Contains(id))
                {
                    aiPicked.Add(id);
                }
            }
        }

        var strictHits = candidates
            .Where(q => RecommendationMatcher.MatchesStrict(q, focusTags, focusKeywords))
            .OrderByDescending(q => ScoreQuestion(q, focusTags, focusKeywords))
            .ThenByDescending(q => q.Id)
            .ToList();

        var final = new List<long>();
        foreach (var id in aiPicked)
        {
            var q = candidates.FirstOrDefault(x => x.Id == id);
            if (q is null)
            {
                continue;
            }

            if (RecommendationMatcher.MatchesStrict(q, focusTags, focusKeywords) && !final.Contains(id))
            {
                final.Add(id);
            }
        }

        foreach (var q in strictHits)
        {
            if (final.Count >= TargetCount)
            {
                break;
            }

            if (!final.Contains(q.Id))
            {
                final.Add(q.Id);
            }
        }

        if (final.Count < TargetCount)
        {
            var relaxed = candidates
                .Where(q => RecommendationMatcher.MatchesRelaxed(q, focusTags, focusKeywords))
                .OrderByDescending(q => ScoreQuestion(q, focusTags, focusKeywords))
                .ThenByDescending(q => q.Id)
                .ToList();
            foreach (var q in relaxed)
            {
                if (final.Count >= TargetCount)
                {
                    break;
                }

                if (!final.Contains(q.Id))
                {
                    final.Add(q.Id);
                }
            }
        }

        if (final.Count < TargetCount)
        {
            foreach (var q in candidates.OrderByDescending(x => x.Id))
            {
                if (final.Count >= TargetCount)
                {
                    break;
                }

                if (!final.Contains(q.Id))
                {
                    final.Add(q.Id);
                }
            }
        }

        if (final.Count < TargetCount)
        {
            throw new InvalidOperationException("无法凑齐推荐题量。");
        }

        return new QuestionRecommendationDto
        {
            Rationale = string.IsNullOrWhiteSpace(parsed.Rationale)
                ? "模型未给出理由。"
                : parsed.Rationale.Trim(),
            FocusTags = focusTags,
            FocusKeywords = focusKeywords,
            RecommendedQuestionIds = final.Take(TargetCount).ToList()
        };
    }

    private static int ScoreQuestion(Question q, IReadOnlyList<string> focusTags, IReadOnlyList<string> focusKeywords)
    {
        var s = 0;
        foreach (var t in focusTags)
        {
            if (RecommendationMatcher.TagFieldsContain(q, t))
            {
                s += 3;
            }
        }

        foreach (var k in focusKeywords)
        {
            if (RecommendationMatcher.KeywordHit(q, k))
            {
                s += 2;
            }
        }

        return s;
    }

    private async Task<QuestionRecommendationDto> FallbackLocalAsync(
        AppDbContext db,
        long userId,
        QuestionRecommendationRequest request,
        HashSet<long> wrongSet,
        IReadOnlyList<Question> candidates,
        IReadOnlyList<Question> contextQuestions,
        CancellationToken cancellationToken)
    {
        var focusTags = InferFocusTagsLocal(contextQuestions);
        var focusKeywords = InferFocusKeywordsLocal(contextQuestions);

        var strict = candidates
            .Where(q => RecommendationMatcher.MatchesStrict(q, focusTags, focusKeywords))
            .OrderByDescending(q => ScoreQuestion(q, focusTags, focusKeywords))
            .ThenByDescending(q => q.Id)
            .ToList();

        var final = strict.Select(q => q.Id).Take(TargetCount).ToList();
        if (final.Count < TargetCount)
        {
            var relaxed = candidates
                .Where(q => RecommendationMatcher.MatchesRelaxed(q, focusTags, focusKeywords))
                .OrderByDescending(q => q.Id)
                .ToList();
            foreach (var q in relaxed)
            {
                if (final.Count >= TargetCount)
                {
                    break;
                }

                if (!final.Contains(q.Id))
                {
                    final.Add(q.Id);
                }
            }
        }

        if (final.Count < TargetCount)
        {
            var more = await db.Questions.AsNoTracking()
                .Where(q => q.IsEnabled && !wrongSet.Contains(q.Id))
                .Where(q => request.DomainScope == null || q.Domain == request.DomainScope.Value)
                .OrderByDescending(q => q.Id)
                .Select(q => q.Id)
                .Take(TargetCount)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var id in more)
            {
                if (final.Count >= TargetCount)
                {
                    break;
                }

                if (!final.Contains(id))
                {
                    final.Add(id);
                }
            }
        }

        return new QuestionRecommendationDto
        {
            Rationale =
                "[本地回退] 云端不可用或候选不足。已按错题 TopicTags/TopicKeywords 与知识点在题库中筛选，并排除错题本题目。",
            FocusTags = focusTags,
            FocusKeywords = focusKeywords,
            RecommendedQuestionIds = final.Take(TargetCount).ToList()
        };
    }

    private static IReadOnlyList<string> InferFocusTagsLocal(IReadOnlyList<Question> contextQuestions)
    {
        var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in contextQuestions)
        {
            foreach (var t in RecommendationMatcher.Tokenize(q.TopicTags))
            {
                bag[t] = bag.TryGetValue(t, out var c) ? c + 1 : 1;
            }

            foreach (var t in RecommendationMatcher.Tokenize(q.KnowledgeTags))
            {
                bag[t] = bag.TryGetValue(t, out var c) ? c + 1 : 1;
            }
        }

        return bag
            .OrderByDescending(kv => kv.Value)
            .Take(6)
            .Select(kv => kv.Key)
            .ToList();
    }

    private static IReadOnlyList<string> InferFocusKeywordsLocal(IReadOnlyList<Question> contextQuestions)
    {
        var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in contextQuestions)
        {
            foreach (var t in RecommendationMatcher.Tokenize(q.TopicKeywords))
            {
                if (t.Length >= 2)
                {
                    bag[t] = bag.TryGetValue(t, out var c) ? c + 1 : 1;
                }
            }
        }

        return bag
            .OrderByDescending(kv => kv.Value)
            .Take(6)
            .Select(kv => kv.Key)
            .ToList();
    }

    private sealed class ArkRecommendationPayload
    {
        public string? Rationale { get; set; }
        public List<string>? FocusTags { get; set; }
        public List<string>? FocusKeywords { get; set; }
        public List<long>? RecommendedQuestionIds { get; set; }
    }
}

/// <summary>
/// 题目与 FocusTags/FocusKeywords 的匹配工具（严格：标签维与关键词维同时满足各自约束；宽松：任一侧命中）。
/// </summary>
internal static class RecommendationMatcher
{
    private static readonly char[] Delims = { ',', '，', ';', '；', '|', '、' };

    /// <summary>
    /// 将模型或配置中的字符串列表规范化（去空、去重）。
    /// </summary>
    public static IReadOnlyList<string> NormalizeTokens(IEnumerable<string>? items)
    {
        if (items is null)
        {
            return Array.Empty<string>();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var x in items)
        {
            var t = (x ?? string.Empty).Trim();
            if (t.Length > 0)
            {
                set.Add(t);
            }
        }

        return set.ToList();
    }

    /// <summary>
    /// 拆分题目侧多值字段。
    /// </summary>
    public static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        foreach (var p in text.Split(Delims, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (p.Length > 0)
            {
                yield return p;
            }
        }
    }

    /// <summary>
    /// 严格匹配：无标签要求或任一标签命中；无关键词要求或任一关键词命中；两者同时成立。
    /// </summary>
    public static bool MatchesStrict(Question q, IReadOnlyList<string> focusTags, IReadOnlyList<string> focusKeywords)
    {
        var tagOk = focusTags.Count == 0 || focusTags.Any(t => TagFieldsContain(q, t));
        var kwOk = focusKeywords.Count == 0 || focusKeywords.Any(k => KeywordHit(q, k));
        return tagOk && kwOk;
    }

    /// <summary>
    /// 宽松匹配：至少一侧有要求时，标签或关键词任一命中即可。
    /// </summary>
    public static bool MatchesRelaxed(Question q, IReadOnlyList<string> focusTags, IReadOnlyList<string> focusKeywords)
    {
        if (focusTags.Count == 0 && focusKeywords.Count == 0)
        {
            return true;
        }

        var tagHit = focusTags.Any(t => TagFieldsContain(q, t));
        var kwHit = focusKeywords.Any(k => KeywordHit(q, k));
        return tagHit || kwHit;
    }

    /// <summary>
    /// 判断分类标签 token 是否出现在题目的 TopicTags 或 KnowledgeTags 分词中。
    /// </summary>
    public static bool TagFieldsContain(Question q, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        token = token.Trim();
        foreach (var bucket in new[] { q.TopicTags, q.KnowledgeTags })
        {
            foreach (var t in Tokenize(bucket))
            {
                if (t.Equals(token, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (t.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                    token.Contains(t, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 判断关键词是否出现在题干、TopicKeywords 或 KnowledgeTags 中。
    /// </summary>
    public static bool KeywordHit(Question q, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return false;
        }

        keyword = keyword.Trim();
        if (q.Stem.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var hay in new[] { q.TopicKeywords, q.KnowledgeTags, q.TopicTags })
        {
            if (!string.IsNullOrEmpty(hay) && hay.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
