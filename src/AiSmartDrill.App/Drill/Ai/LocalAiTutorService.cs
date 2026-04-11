using AiSmartDrill.App.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 本地占位实现的 AI 错题解析服务：不发起外网请求，但保留完整调用链与日志点。
/// </summary>
public sealed class LocalAiTutorService : IAiTutorService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalAiTutorService> _logger;

    /// <summary>
    /// 初始化 <see cref="LocalAiTutorService"/> 的新实例。
    /// </summary>
    /// <param name="configuration">应用配置（读取 Endpoint/Key 占位）。</param>
    /// <param name="logger">日志记录器。</param>
    public LocalAiTutorService(IConfiguration configuration, ILogger<LocalAiTutorService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WrongQuestionInsightDto>> AnalyzeWrongQuestionsAsync(
        IReadOnlyList<WrongQuestionInsightDto> wrongItems,
        CancellationToken cancellationToken = default)
    {
        // 记录配置中的占位端点，便于将来替换为真实网关时对照。
        var endpoint = _configuration["Ai:Endpoint"] ?? string.Empty;
        _logger.LogInformation("AI 错题解析（占位）：Endpoint={Endpoint}, Count={Count}", endpoint, wrongItems.Count);

        // 占位规则：基于题型生成模板化解析，确保 UI 有可展示内容。
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
                    $"建议步骤：1) 回顾知识点标签；2) 对照标准答案“{item.StandardAnswer}”；3) 用自己的话复述结论。";

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

        return Task.FromResult<IReadOnlyList<WrongQuestionInsightDto>>(results);
    }
}
