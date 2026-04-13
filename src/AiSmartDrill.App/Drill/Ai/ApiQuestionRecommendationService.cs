using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于实际API的题目推荐服务：调用外部AI API进行智能推荐。
/// </summary>
public sealed class ApiQuestionRecommendationService : IQuestionRecommendationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiQuestionRecommendationService> _logger;

    /// <summary>
    /// 初始化 <see cref="ApiQuestionRecommendationService"/> 的新实例。
    /// </summary>
    /// <param name="dbFactory">数据库上下文工厂。</param>
    /// <param name="httpClientFactory">HTTP客户端工厂。</param>
    /// <param name="configuration">配置。</param>
    /// <param name="logger">日志记录器。</param>
    public ApiQuestionRecommendationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ApiQuestionRecommendationService> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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
        var endpoint = _configuration["Ai:Endpoint"] ?? "https://ark.cn-beijing.volces.com/api/v3/chat/completions";
        var apiKey = _configuration["Ai:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("AI API 密钥未设置");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var weakTagsString = string.Join(", ", summary.WeakTags);

        var requestBody = new
        {
            model = "Doubao-Seed-1.8-251128", // 替换为您的模型ID
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "教育AI助手：根据学生学习数据推荐8道适合的题目。"
                },
                new
                {
                    role = "user",
                    content = string.Format("推荐题目：用户ID={0},总答题={1},正确={2},错题={3},弱项={4}\n\n严格要求：仅返回JSON格式，包含Rationale和RecommendedQuestionIds两个字段，不要包含其他任何文本。\n示例：{{\"Rationale\":\"推荐理由\",\"RecommendedQuestionIds\":[1,2,3,4,5,6,7,8]}}" , summary.UserId, summary.TotalAttempts, summary.CorrectAttempts, summary.WrongBookCount, weakTagsString)
                }
            },
            temperature = 0.3,
            max_tokens = 500
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var arkResponse = JsonSerializer.Deserialize<ArkChatResponse>(responseContent);

        if (arkResponse?.choices?.FirstOrDefault()?.message?.content == null)
        {
            throw new InvalidOperationException("AI API 返回格式错误");
        }

        // 尝试解析AI返回的JSON
        try
        {
            var recommendation = JsonSerializer.Deserialize<QuestionRecommendationDto>(arkResponse.choices.First().message.content);
            return recommendation ?? new QuestionRecommendationDto();
        }
        catch
        {
            // 如果AI返回的不是JSON格式，使用默认推荐
            return await FallbackToLocalRecommendationAsync(null, summary.UserId, cancellationToken).ConfigureAwait(false);
        }
    }

    private class ArkChatResponse
    {
        public List<Choice>? choices { get; set; }

        public class Choice
        {
            public Message? message { get; set; }

            public class Message
            {
                public string? content { get; set; }
            }
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