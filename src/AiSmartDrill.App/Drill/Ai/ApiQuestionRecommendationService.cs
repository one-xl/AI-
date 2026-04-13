using System.Text.Json;
using AiSmartDrill.App.Drill.Ai.Ark;
using AiSmartDrill.App.Drill.Ai.Client;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于火山方舟 Chat Completions 的题目推荐：根据学习摘要请求 JSON，解析失败时回退本地选题。
/// </summary>
public sealed class ApiQuestionRecommendationService : IQuestionRecommendationService
{
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
    public async Task<QuestionRecommendationDto> RecommendAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var wrongEntries = await db.WrongBookEntries
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var answerRecords = await db.AnswerRecords
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var performanceSummary = new UserPerformanceSummary
        {
            UserId = userId,
            TotalAttempts = answerRecords.Count,
            CorrectAttempts = answerRecords.Count(a => a.IsCorrect),
            WrongBookCount = wrongEntries.Count,
            WeakTags = await GetWeakTags(db, userId, cancellationToken).ConfigureAwait(false)
        };

        var candidateQuestionIds = await db.Questions.AsNoTracking()
            .Where(q => q.IsEnabled)
            .OrderBy(q => q.Id)
            .Select(q => q.Id)
            .Take(400)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidateQuestionIds.Count < 8)
        {
            _logger.LogWarning("启用题目不足 8 道，跳过 Ark，使用本地推荐");
            return await FallbackLocalAsync(db, userId, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var dto = await CallArkAsync(performanceSummary, candidateQuestionIds, cancellationToken).ConfigureAwait(false);
            _trace.Set("recommend:ark", true);
            _logger.LogInformation("AI 题目推荐（Ark）：UserId={UserId}, Count={Count}", userId, dto.RecommendedQuestionIds.Count);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ark 题目推荐失败，回退本地");
            _trace.Set("recommend:local-fallback", false);
            return await FallbackLocalAsync(db, userId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<string>> GetWeakTags(AppDbContext db, long userId, CancellationToken cancellationToken)
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
            foreach (var t in tags.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (tagCount.TryGetValue(t, out var count))
                    tagCount[t] = count + 1;
                else
                    tagCount[t] = 1;
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
        IReadOnlyList<long> candidateQuestionIds,
        CancellationToken cancellationToken)
    {
        var weakTagsString = string.Join(", ", summary.WeakTags);
        var catalog = string.Join(", ", candidateQuestionIds);

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "system",
                Content =
                    "你是教育场景助手。只输出一个 JSON 对象，不要 Markdown，不要解释。字段：Rationale(string)、RecommendedQuestionIds(number[])，须推荐恰好 8 个题目 Id。" +
                    "RecommendedQuestionIds 中的每一个 Id 必须来自用户消息中的「可选题目 Id」列表，禁止编造列表中不存在的 Id。"
            },
            new ChatMessage
            {
                Role = "user",
                Content =
                    $"学习摘要：用户ID={summary.UserId}, 总答题={summary.TotalAttempts}, 正确={summary.CorrectAttempts}, 错题条目={summary.WrongBookCount}, 弱项标签={weakTagsString}\n" +
                    $"可选题目 Id（必须从中选取 8 个）：{catalog}"
            }
        };

        var response = await _chat.GenerateCompletionAsync(messages, null, cancellationToken).ConfigureAwait(false);
        var raw = ArkAssistantReply.GetPrimaryText(response);
        var json = ArkModelOutputParsing.ExtractFirstJsonValue(raw);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("模型返回为空。");

        var dto = JsonSerializer.Deserialize<QuestionRecommendationDto>(json, ArkChatJsonDefaults.ModelPayloadOptions);
        if (dto == null || dto.RecommendedQuestionIds.Count == 0)
            throw new InvalidOperationException("JSON 无效或未包含题目 Id。");

        var allowed = candidateQuestionIds.ToHashSet();
        var picked = new List<long>();
        foreach (var id in dto.RecommendedQuestionIds)
        {
            if (picked.Count >= 8)
                break;
            if (allowed.Contains(id) && !picked.Contains(id))
                picked.Add(id);
        }

        foreach (var id in candidateQuestionIds)
        {
            if (picked.Count >= 8)
                break;
            if (!picked.Contains(id))
                picked.Add(id);
        }

        if (picked.Count < 8)
            throw new InvalidOperationException("无法凑齐 8 道有效题目 Id。");

        return new QuestionRecommendationDto
        {
            Rationale = dto.Rationale,
            RecommendedQuestionIds = picked.Take(8).ToList()
        };
    }

    private async Task<QuestionRecommendationDto> FallbackLocalAsync(AppDbContext db, long userId, CancellationToken cancellationToken)
    {
        var recentQuestions = await db.Questions.AsNoTracking()
            .Where(q => q.IsEnabled)
            .OrderByDescending(q => q.Id)
            .Take(8)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new QuestionRecommendationDto
        {
            Rationale = "[本地回退·未调用方舟] 云端推荐不可用或解析失败，已改为从题库按 Id 取最近题目。",
            RecommendedQuestionIds = recentQuestions
        };
    }
}
