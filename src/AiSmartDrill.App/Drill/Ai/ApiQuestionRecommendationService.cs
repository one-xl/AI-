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
/// 基于火山方舟的题目推荐：结合错题上下文推断主知识点（<c>FocusKnowledgePoint</c>），在候选集中优先推荐
/// 同领域、同细知识点的题目，并以 FocusTags/FocusKeywords 辅助筛选，排除错题本中的题目。
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

        var effectiveDomain = request.DomainScope
                              ?? KnowledgePointInference.InferMajorityDomain(contextQuestions)
                              ?? await KnowledgePointCatalogQuery.InferDominantDomainFromWrongBookAsync(
                                  db,
                                  userId,
                                  request,
                                  cancellationToken)
                                  .ConfigureAwait(false);
        var candidates = await (
                from q in db.Questions.AsNoTracking()
                where q.IsEnabled && !wrongSet.Contains(q.Id)
                where effectiveDomain == null || q.Domain == effectiveDomain.Value
                orderby q.Id
                select q)
            .Take(MaxCatalogLines)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidates.Count < TargetCount)
        {
            _logger.LogWarning("候选题不足 {Need}（当前 {Have}），使用本地推荐。", TargetCount, candidates.Count);
            return await FallbackLocalAsync(
                    db,
                    userId,
                    request,
                    wrongSet,
                    candidates,
                    contextQuestions,
                    performanceSummary,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            var dto = await CallArkAsync(
                    db,
                    performanceSummary,
                    request,
                    effectiveDomain,
                    candidates,
                    contextQuestions,
                    wrongQuestionIds,
                    cancellationToken)
                .ConfigureAwait(false);
            _trace.Set("recommend:ark", true);
            _logger.LogInformation("AI 题目推荐（Ark）：UserId={UserId}, Count={Count}", userId, dto.RecommendedQuestionIds.Count);
            return dto;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ark 题目推荐失败，回退本地");
            _trace.Set("recommend:local-fallback", false);
            return await FallbackLocalAsync(
                    db,
                    userId,
                    request,
                    wrongSet,
                    candidates,
                    contextQuestions,
                    performanceSummary,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static Task<UserPerformanceSummary> BuildPerformanceSummaryAsync(
        AppDbContext db,
        long userId,
        CancellationToken cancellationToken) =>
        UserPerformanceSummaryFactory.CreateAsync(db, userId, cancellationToken);

    private async Task<QuestionRecommendationDto> CallArkAsync(
        AppDbContext db,
        UserPerformanceSummary summary,
        QuestionRecommendationRequest request,
        QuestionDomain? effectiveDomain,
        IReadOnlyList<Question> candidates,
        IReadOnlyList<Question> contextQuestions,
        IReadOnlyList<long> wrongQuestionIds,
        CancellationToken cancellationToken)
    {
        const int catalogAlignPool = 200;
        IReadOnlyList<string> fullKpCatalog = effectiveDomain is { } catalogDom
            ? await KnowledgePointCatalogQuery.LoadOrderedByFrequencyAsync(
                    db,
                    catalogDom,
                    catalogAlignPool,
                    cancellationToken)
                .ConfigureAwait(false)
            : Array.Empty<string>();

        var weakForFilter = MergeExternalWeak(summary.WeakKnowledgePoints, request.ExternalSkillHints);
        var anchorForCatalogSubset = KnowledgePointInference.InferPrimaryKnowledgePoint(contextQuestions)
                                     ?? (request.ExternalSkillHints.Count > 0 ? request.ExternalSkillHints[0] : null)
                                     ?? (summary.WeakKnowledgePoints.Count > 0 ? summary.WeakKnowledgePoints[0] : null);
        var kpCatalogForPrompt = KnowledgePointCatalogQuery.FilterCatalogForPrompt(
            fullKpCatalog,
            anchorForCatalogSubset,
            weakForFilter,
            KnowledgePointCatalogQuery.DefaultPromptCatalogLimit);

        var kpCatalogNote = kpCatalogForPrompt.Count > 0
            ? "题库已有知识点短语（仅当前推荐领域，且为与错题主知识点/弱项细知识点相关的子集；FocusKnowledgePoint 请优先与下列之一对齐；归一化可与同领域其他已入库短语一致；候选表 Id 仍须从下方候选行选取）：\n" +
              string.Join("、", kpCatalogForPrompt) + "\n\n"
            : (effectiveDomain is not null
                ? "当前领域下暂无与锚点强相关的已入库短语子集，请据错题、弱项与候选表自行给出 FocusKnowledgePoint（勿混用其他学科体系）。\n\n"
                : "当前无法确定推荐领域，不向本题注入跨领域知识点短语表；请仅据候选表与错题上下文给出 FocusKnowledgePoint。\n\n");

        var careerPathBlock = string.Empty;
        if (request.ExternalSkillHints.Count > 0)
        {
            careerPathBlock =
                "【CareerPath 岗位技能包】\n" +
                "期望难度：" + (request.ExternalDifficultyHint ?? "未指定") + "\n" +
                "岗位简述：" + (request.ExternalJobSummary ?? string.Empty) + "\n" +
                "目标技能点：" + string.Join("、", request.ExternalSkillHints) + "\n\n";
        }

        var weakKp = string.Join(", ", summary.WeakKnowledgePoints);
        var weakTopics = string.Join(", ", summary.WeakTopicTags);
        var domainNote = request.DomainScope is { } domReq
            ? $"推荐领域限定为：{domReq}（候选表仅含该领域且已排除错题本中该领域下的题目）。"
            : effectiveDomain is { } domInf
                ? $"推荐领域根据错题上下文或错题本推断为：{domInf}（候选表仅含该领域；知识点短语表仅含该领域）。"
                : "推荐领域：不限（候选表已排除用户错题本中的全部题目；未注入跨领域知识点短语表）。";

        var wrongLines = new StringBuilder();
        if (contextQuestions.Count > 0)
        {
            wrongLines.AppendLine("错题上下文（推断 FocusTags/FocusKeywords 与 FocusKnowledgePoint；勿推荐下列 Id）：");
            foreach (var q in contextQuestions.Take(24))
            {
                var stem = q.Stem.Length > 72 ? q.Stem[..72] + "…" : q.Stem;
                wrongLines.AppendLine(
                    $"Id={q.Id}; Domain={q.Domain}; TopicTags={q.TopicTags}; TopicKeywords={q.TopicKeywords}; PrimaryKnowledgePoint={q.PrimaryKnowledgePoint}; KnowledgeTags={q.KnowledgeTags}; Stem={stem}");
            }
        }
        else
        {
            wrongLines.AppendLine("当前无错题条目或领域筛选后无错题，请结合弱项细知识点与候选表自行给出 FocusKnowledgePoint 与辅助标签/关键词。");
            if (wrongQuestionIds.Count > 0)
            {
                wrongLines.AppendLine("用户错题本题目 Id（勿推荐）：" + string.Join(", ", wrongQuestionIds.Take(40)));
            }
        }

        var catalog = new StringBuilder();
        catalog.AppendLine(
            "候选题目表（每行：Id|Domain|TopicTags|PrimaryKnowledgePoint|KnowledgeTags|TopicKeywords|Stem前56字）：");
        foreach (var q in candidates)
        {
            var stem = q.Stem.Length > 56 ? q.Stem[..56] + "…" : q.Stem;
            catalog.AppendLine(
                $"{q.Id}|{q.Domain}|{q.TopicTags}|{q.PrimaryKnowledgePoint}|{q.KnowledgeTags}|{q.TopicKeywords}|{stem}");
        }

        var systemPrompt =
            "你是教育场景助手。只输出一个 JSON 对象，不要 Markdown，不要解释文字。\n" +
            "字段：Rationale(string)、FocusKnowledgePoint(string)、FocusTags(string[])、FocusKeywords(string[])、RecommendedQuestionIds(number[])。Rationale 一句话，不超过 48 字。\n" +
            "FocusKnowledgePoint 为单个主知识点短语：须优先与「用户消息中的已有知识点短语子集」某项一致或互为包含，且须属于当前推荐领域；并与候选表中某题的 PrimaryKnowledgePoint 或 KnowledgeTags 分词一致或可互相包含；所有 RecommendedQuestionIds 应优先命中同一 FocusKnowledgePoint。\n" +
            "FocusTags/FocusKeywords 为辅助筛选信号：贴近 TopicTags/KnowledgeTags/TopicKeywords。\n" +
            "RecommendedQuestionIds 须恰好 " + TargetCount + " 个，且每个 Id 必须出现在候选表中；不得包含错题上下文或「勿推荐」中的 Id。\n" +
            "若领域已限定，只推荐该 Domain 的题。";
        if (request.ExternalSkillHints.Count > 0)
        {
            systemPrompt +=
                "\n若用户消息含 CareerPath 技能包段落，FocusKnowledgePoint 与 RecommendedQuestionIds 须优先对齐「目标技能点」，并参考岗位简述与期望难度。";
        }

        var messages = new List<ChatMessage>
        {
            new()
            {
                Role = "system",
                Content = systemPrompt
            },
            new()
            {
                Role = "user",
                Content =
                    domainNote + "\n" +
                    kpCatalogNote +
                    careerPathBlock +
                    $"学习摘要：总答题={summary.TotalAttempts}, 正确={summary.CorrectAttempts}, 错题条目={summary.WrongBookCount}, 弱项模块标签={weakTopics}, 弱项细知识点={weakKp}\n\n" +
                    wrongLines + "\n" +
                    catalog
            }
        };

        var response = await _chat
            .GenerateCompletionAsync(messages, null, AiCompletionTokenBudgets.RecommendationJson, cancellationToken)
            .ConfigureAwait(false);
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
        var modelKp = string.IsNullOrWhiteSpace(parsed.FocusKnowledgePoint) ? null : parsed.FocusKnowledgePoint.Trim();
        var inferredKp = KnowledgePointInference.InferPrimaryKnowledgePoint(contextQuestions);
        var fallbackKp = summary.WeakKnowledgePoints.Count > 0 ? summary.WeakKnowledgePoints[0] : null;
        var externalKp = request.ExternalSkillHints.Count > 0 ? request.ExternalSkillHints[0] : null;
        var resolvedKpRaw = modelKp ?? inferredKp ?? externalKp ?? fallbackKp;
        var resolvedKp = KnowledgePointCatalogQuery.AlignFocusKnowledgePoint(resolvedKpRaw, fullKpCatalog);

        var allowed = candidates.Select(q => q.Id).ToHashSet();
        var aiPicked = new List<long>();
        if (parsed.RecommendedQuestionIds is not null)
        {
            foreach (var id in parsed.RecommendedQuestionIds)
            {
                if (aiPicked.Count >= TargetCount * 2)
                {
                    break;
                }

                if (allowed.Contains(id) && !aiPicked.Contains(id))
                {
                    aiPicked.Add(id);
                }
            }
        }

        var (final, relaxedKp) = RecommendationIdAssembly.AssembleRecommendedIds(
            candidates,
            focusTags,
            focusKeywords,
            resolvedKp,
            aiPicked,
            TargetCount);
        if (final.Count < TargetCount)
        {
            throw new InvalidOperationException("无法凑齐推荐题量。");
        }

        var rationale = string.IsNullOrWhiteSpace(parsed.Rationale) ? "模型未给出理由。" : parsed.Rationale.Trim();
        if (relaxedKp)
        {
            rationale = string.IsNullOrEmpty(rationale)
                ? "同知识点候选不足，已放宽知识点约束。"
                : $"{rationale}（同知识点候选不足，已部分放宽）";
        }

        return new QuestionRecommendationDto
        {
            Rationale = rationale,
            FocusKnowledgePoint = resolvedKp,
            FocusTags = focusTags,
            FocusKeywords = focusKeywords,
            RecommendedQuestionIds = final.Take(TargetCount).ToList()
        };
    }

    private async Task<QuestionRecommendationDto> FallbackLocalAsync(
        AppDbContext db,
        long userId,
        QuestionRecommendationRequest request,
        HashSet<long> wrongSet,
        IReadOnlyList<Question> candidates,
        IReadOnlyList<Question> contextQuestions,
        UserPerformanceSummary performanceSummary,
        CancellationToken cancellationToken)
    {
        var focusTags = MergeTokenLists(InferFocusTagsLocal(contextQuestions), request.ExternalSkillHints);
        var focusKeywords = MergeTokenLists(InferFocusKeywordsLocal(contextQuestions), request.ExternalSkillHints);
        var effectiveDomain = request.DomainScope
                              ?? KnowledgePointInference.InferMajorityDomain(contextQuestions)
                              ?? await KnowledgePointCatalogQuery.InferDominantDomainFromWrongBookAsync(
                                      db,
                                      userId,
                                      request,
                                      cancellationToken)
                                  .ConfigureAwait(false);
        const int catalogAlignPool = 200;
        IReadOnlyList<string> fullKpCatalog = effectiveDomain is { } catalogDom
            ? await KnowledgePointCatalogQuery.LoadOrderedByFrequencyAsync(
                    db,
                    catalogDom,
                    catalogAlignPool,
                    cancellationToken)
                .ConfigureAwait(false)
            : Array.Empty<string>();
        var resolvedKpRaw = KnowledgePointInference.InferPrimaryKnowledgePoint(contextQuestions)
                            ?? (request.ExternalSkillHints.Count > 0 ? request.ExternalSkillHints[0] : null)
                            ?? (performanceSummary.WeakKnowledgePoints.Count > 0
                                ? performanceSummary.WeakKnowledgePoints[0]
                                : null);
        var resolvedKp = KnowledgePointCatalogQuery.AlignFocusKnowledgePoint(resolvedKpRaw, fullKpCatalog);

        var (final, relaxedKp) = RecommendationIdAssembly.AssembleRecommendedIds(
            candidates,
            focusTags,
            focusKeywords,
            resolvedKp,
            Array.Empty<long>(),
            TargetCount);
        if (final.Count < TargetCount)
        {
            var moreIds = await db.Questions.AsNoTracking()
                .Where(q => q.IsEnabled && !wrongSet.Contains(q.Id))
                .Where(q => effectiveDomain == null || q.Domain == effectiveDomain.Value)
                .OrderByDescending(q => q.Id)
                .Select(q => q.Id)
                .Take(TargetCount * 2)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var id in moreIds)
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

        var rationale =
            "[本地回退] 云端不可用或候选不足。已按主知识点与 TopicTags/TopicKeywords 筛选，并排除错题本题目。";
        if (relaxedKp)
        {
            rationale += "（同知识点候选不足，已部分放宽）";
        }

        return new QuestionRecommendationDto
        {
            Rationale = rationale,
            FocusKnowledgePoint = resolvedKp,
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
                if (!KnowledgeTagStopwords.IsStopword(t))
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

    /// <summary>
    /// 弱项知识点与 CareerPath 外部技能点合并（外部优先），供子集筛选。
    /// </summary>
    private static List<string> MergeExternalWeak(IReadOnlyList<string> weak, IReadOnlyList<string> external)
    {
        var r = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var x in external)
        {
            var t = (x ?? string.Empty).Trim();
            if (t.Length > 0 && seen.Add(t))
            {
                r.Add(t);
            }
        }

        foreach (var x in weak)
        {
            var t = (x ?? string.Empty).Trim();
            if (t.Length > 0 && seen.Add(t))
            {
                r.Add(t);
            }
        }

        return r;
    }

    /// <summary>
    /// 将推断出的焦点词与外部技能点去重合并。
    /// </summary>
    private static IReadOnlyList<string> MergeTokenLists(IReadOnlyList<string> baseTokens, IReadOnlyList<string> extra)
    {
        if (extra.Count == 0)
        {
            return baseTokens;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        foreach (var x in baseTokens)
        {
            var t = (x ?? string.Empty).Trim();
            if (t.Length > 0 && seen.Add(t))
            {
                list.Add(t);
            }
        }

        foreach (var x in extra)
        {
            var t = (x ?? string.Empty).Trim();
            if (t.Length > 0 && seen.Add(t))
            {
                list.Add(t);
            }
        }

        return list;
    }

    private sealed class ArkRecommendationPayload
    {
        public string? Rationale { get; set; }
        public string? FocusKnowledgePoint { get; set; }
        public List<string>? FocusTags { get; set; }
        public List<string>? FocusKeywords { get; set; }
        public List<long>? RecommendedQuestionIds { get; set; }
    }
}
