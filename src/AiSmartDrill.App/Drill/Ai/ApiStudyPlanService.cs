using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于Ark API的学习计划生成服务：调用火山引擎Ark API生成个性化刷题计划。
/// </summary>
public sealed class ApiStudyPlanService : IStudyPlanService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiStudyPlanService> _logger;

    /// <summary>
    /// 初始化 <see cref="ApiStudyPlanService"/> 的新实例。
    /// </summary>
    /// <param name="httpClientFactory">HTTP客户端工厂。</param>
    /// <param name="configuration">配置。</param>
    /// <param name="logger">日志记录器。</param>
    public ApiStudyPlanService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ApiStudyPlanService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StudyPlanDto> GeneratePlanAsync(UserPerformanceSummary summary, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI 学习计划生成（API）：UserId={UserId}", summary.UserId);

        try
        {
            return await CallAiApiAsync(summary, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI API 调用失败，回退到本地计划生成");
            // 回退到本地计划生成
            return FallbackToLocalPlan(summary);
        }
    }

    private async Task<StudyPlanDto> CallAiApiAsync(UserPerformanceSummary summary, CancellationToken cancellationToken)
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
            model = "ep-20260413170846-xqxj7", // 替换为您的模型ID
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "教育AI助手：根据学生学习数据生成个性化刷题计划。"
                },
                new
                {
                    role = "user",
                    content = $"生成计划：用户ID={summary.UserId},总答题={summary.TotalAttempts},正确={summary.CorrectAttempts},错题={summary.WrongBookCount},弱项={weakTagsString}\n返回JSON：{\"Title\":\"计划标题\",\"DailyQuestionQuota\":5,\"FocusKnowledgeTags\":[\"知识点1\",\"知识点2\"],\"PhaseDays\":7,\"Notes\":\"计划说明\"}"
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
            var planResult = JsonSerializer.Deserialize<StudyPlanDto>(arkResponse.choices.First().message.content);
            return planResult ?? FallbackToLocalPlan(summary);
        }
        catch
        {
            // 如果AI返回的不是JSON格式，使用默认计划
            return FallbackToLocalPlan(summary);
        }
    }

    private StudyPlanDto FallbackToLocalPlan(UserPerformanceSummary summary)
    {
        // 基于用户数据生成默认计划
        var dailyQuota = summary.TotalAttempts > 0 ? Math.Max(5, summary.TotalAttempts / 10) : 5;
        var phaseDays = summary.WrongBookCount > 10 ? 14 : 7;

        return new StudyPlanDto
        {
            Title = "个性化学习计划",
            DailyQuestionQuota = dailyQuota,
            FocusKnowledgeTags = summary.WeakTags.Count > 0 ? summary.WeakTags : new List<string> { "基础知识" },
            PhaseDays = phaseDays,
            Notes = "基于您的学习数据生成的默认计划。建议每天坚持练习，重点关注薄弱知识点。"
        };
    }

    private class ArkChatResponse
    {
        public List<Choice> choices { get; set; }

        public class Choice
        {
            public Message message { get; set; }

            public class Message
            {
                public string content { get; set; }
            }
        }
    }
}