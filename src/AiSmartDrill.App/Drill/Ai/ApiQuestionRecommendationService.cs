using AiSmartDrill.App.Infrastructure;
using AiSmartDrill.App.Drill.Ai.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于实际API的题目推荐服务：调用外部AI API进行智能推荐。
/// </summary>
public sealed class ApiQuestionRecommendationService : IQuestionRecommendationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<ApiQuestionRecommendationService> _logger;

    /// <summary>
    /// 初始化 <see cref="ApiQuestionRecommendationService"/> 的新实例。
    /// </summary>
    /// <param name="dbFactory">数据库上下文工厂。</param>
    /// <param name="chatCompletionService">聊天完成服务。</param>
    /// <param name="logger">日志记录器。</param>
    public ApiQuestionRecommendationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IChatCompletionService chatCompletionService,
        ILogger<ApiQuestionRecommendationService> logger)
    {
        _dbFactory = dbFactory;
        _chatCompletionService = chatCompletionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<QuestionRecommendationDto> RecommendAsync(long userId, CancellationToken cancellationToken = default)
    {
        // 1. 从数据库获取用户的学习数据
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // 获取用户的错题记录
        var wrongEntries = await db.WrongBookEntries
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // 获取用户的答题记录
        var answerRecords = await db.AnswerRecords
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // 构建用户表现摘要
        var performanceSummary = new UserPerformanceSummary
        {
            UserId = userId,
            TotalAttempts = answerRecords.Count,
            CorrectAttempts = answerRecords.Count(a => a.IsCorrect),
            WrongBookCount = wrongEntries.Count,
            WeakTags = await GetWeakTags(db, userId, cancellationToken).ConfigureAwait(false)
        };

        // 2. 调用AI API获取推荐
        try
        {
            var recommendation = await CallAiApiAsync(performanceSummary, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("AI 题目推荐（API）：UserId={UserId}, Picked={Count}", userId, recommendation.RecommendedQuestionIds.Count);
            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI API 调用失败，回退到本地推荐");
            // 回退到本地推荐
            return await FallbackToLocalRecommendationAsync(db, userId, cancellationToken).ConfigureAwait(false);
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

        // 返回出现次数最多的前5个标签
        return tagCount
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => kv.Key)
            .ToList();
    }

    private async Task<QuestionRecommendationDto> CallAiApiAsync(UserPerformanceSummary summary, CancellationToken cancellationToken)
    {
        var weakTagsString = string.Join(", ", summary.WeakTags);

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "system",
                Content = "教育AI助手：根据学生学习数据推荐8道适合的题目。"
            },
            new ChatMessage
            {
                Role = "user",
                Content = string.Format("推荐题目：用户ID={0},总答题={1},正确={2},错题={3},弱项={4}\n\n严格要求：仅返回JSON格式，包含Rationale和RecommendedQuestionIds两个字段，不要包含其他任何文本。\n示例：{{\"Rationale\":\"推荐理由\",\"RecommendedQuestionIds\":[1,2,3,4,5,6,7,8]}}" , summary.UserId, summary.TotalAttempts, summary.CorrectAttempts, summary.WrongBookCount, weakTagsString)
            }
        };

        var response = await _chatCompletionService.GenerateCompletionAsync(messages, null, cancellationToken).ConfigureAwait(false);

        // 尝试解析AI返回的JSON
        var aiContent = response.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        try
        {
            if (string.IsNullOrEmpty(aiContent))
            {
                throw new InvalidOperationException("AI API 返回内容为空");
            }
            var recommendation = JsonSerializer.Deserialize<QuestionRecommendationDto>(aiContent);
            return recommendation ?? new QuestionRecommendationDto();
        }
        catch
        {
            // 如果AI返回的不是JSON格式，使用默认推荐
            return await FallbackToLocalRecommendationAsync(null, summary.UserId, cancellationToken).ConfigureAwait(false);
        }
    }



    private async Task<QuestionRecommendationDto> FallbackToLocalRecommendationAsync(AppDbContext? db, long userId, CancellationToken cancellationToken)
    {
        // 如果db为null，创建一个新的数据库上下文
        if (db == null)
        {
            await using var newDb = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await FallbackToLocalRecommendationAsync(newDb, userId, cancellationToken).ConfigureAwait(false);
        }

        // 从题库中挑选最近的题目作为回退
        var recentQuestions = await db.Questions.AsNoTracking()
            .Where(q => q.IsEnabled)
            .OrderByDescending(q => q.Id)
            .Take(8)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new QuestionRecommendationDto
        {
            Rationale = "AI API 调用失败，推荐最近入库的题目。",
            RecommendedQuestionIds = recentQuestions
        };
    }
}