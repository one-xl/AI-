using AiSmartDrill.App.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于Ark API的AI错题解析服务：调用火山引擎Ark API进行智能错题分析。
/// </summary>
public sealed class ApiAiTutorService : IAiTutorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiAiTutorService> _logger;

    /// <summary>
    /// 初始化 <see cref="ApiAiTutorService"/> 的新实例。
    /// </summary>
    /// <param name="httpClientFactory">HTTP客户端工厂。</param>
    /// <param name="configuration">配置。</param>
    /// <param name="logger">日志记录器。</param>
    public ApiAiTutorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ApiAiTutorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WrongQuestionInsightDto>> AnalyzeWrongQuestionsAsync(
        IReadOnlyList<WrongQuestionInsightDto> wrongItems,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI 错题解析（API）：Count={Count}", wrongItems.Count);

        try
        {
            var results = new List<WrongQuestionInsightDto>();
            foreach (var item in wrongItems)
            {
                var analysis = await AnalyzeSingleQuestionAsync(item, cancellationToken).ConfigureAwait(false);
                results.Add(analysis);
            }
            return results.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI API 调用失败，回退到本地解析");
            // 回退到本地解析
            return FallbackToLocalAnalysis(wrongItems);
        }
    }

    private async Task<WrongQuestionInsightDto> AnalyzeSingleQuestionAsync(WrongQuestionInsightDto item, CancellationToken cancellationToken)
    {
        var endpoint = _configuration["Ai:Endpoint"] ?? "https://ark.cn-beijing.volces.com/api/v3/chat/completions";
        var apiKey = _configuration["Ai:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("AI API 密钥未设置");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var requestBody = new
        {
            model = "Doubao-Seed-1.8-251128", // 替换为您的模型ID
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "教育AI助手：分析错题，提供错误原因和解题思路。"
                },
                new
                {
                    role = "user",
                    content = string.Format("分析错题：类型={0},题干={1},用户答案={2},标准答案={3}\n\n严格要求：仅返回JSON格式，包含RootCause和SolutionHints两个字段，不要包含其他任何文本。\n示例：{{\"RootCause\":\"错误原因\",\"SolutionHints\":\"解题思路\"}}" , item.Type, item.StemSummary, item.UserAnswer, item.StandardAnswer)
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
            var analysisResult = JsonSerializer.Deserialize<AnalysisResult>(arkResponse.choices.First().message.content);
            return new WrongQuestionInsightDto
            {
                QuestionId = item.QuestionId,
                Type = item.Type,
                StemSummary = item.StemSummary,
                UserAnswer = item.UserAnswer,
                StandardAnswer = item.StandardAnswer,
                RootCause = analysisResult?.RootCause ?? "无法分析错误原因",
                SolutionHints = analysisResult?.SolutionHints ?? "无法提供解题思路"
            };
        }
        catch
        {
            // 如果AI返回的不是JSON格式，直接使用内容
            return new WrongQuestionInsightDto
            {
                QuestionId = item.QuestionId,
                Type = item.Type,
                StemSummary = item.StemSummary,
                UserAnswer = item.UserAnswer,
                StandardAnswer = item.StandardAnswer,
                RootCause = "错误原因分析：" + arkResponse.choices.First().message.content,
                SolutionHints = "解题思路：" + arkResponse.choices.First().message.content
            };
        }
    }

    private IReadOnlyList<WrongQuestionInsightDto> FallbackToLocalAnalysis(IReadOnlyList<WrongQuestionInsightDto> wrongItems)
    {
        // 占位规则：基于题型生成模板化解析
        var results = wrongItems
            .Select(item =>
            {
                var root = item.Type switch
                {
                    QuestionType.SingleChoice => "单选题常见错误来自概念混淆或审题不清。",
                    QuestionType.MultipleChoice => "多选题需要检查是否漏选或多选，建议逐项排除。",
                    QuestionType.TrueFalse => "判断题建议回到定义与边界条件，避免绝对化表述误判。",
                    QuestionType.ShortAnswer => "简答题需要抓住关键词，建议先列提纲再组织语言。",
                    _ => "该题需要结合知识点复盘。"
                };

                var hints =
                    $"建议步骤：1) 回顾知识点标签；2) 对照标准答案\"{item.StandardAnswer}\"；3) 用自己的话复述结论。";

                return new WrongQuestionInsightDto
                {
                    QuestionId = item.QuestionId,
                    Type = item.Type,
                    StemSummary = item.StemSummary,
                    UserAnswer = item.UserAnswer,
                    StandardAnswer = item.StandardAnswer,
                    RootCause = root,
                    SolutionHints = hints
                };
            })
            .ToList()
            .AsReadOnly();

        return results;
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

    private class AnalysisResult
    {
        public string? RootCause { get; set; }
        public string? SolutionHints { get; set; }
    }
}