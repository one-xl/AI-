using System.Text.Json;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Drill.Ai.Ark;
using AiSmartDrill.App.Drill.Ai.Client;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于火山方舟 Chat Completions 的错题解析：单次请求批量返回结构化 JSON，失败时回退本地模板。
/// </summary>
public sealed class ApiAiTutorService : IAiTutorService
{
    private readonly IChatCompletionService _chat;
    private readonly ILogger<ApiAiTutorService> _logger;
    private readonly AiCallTrace _trace;

    /// <summary>
    /// 初始化 <see cref="ApiAiTutorService"/>。
    /// </summary>
    public ApiAiTutorService(
        IChatCompletionService chatCompletionService,
        ILogger<ApiAiTutorService> logger,
        AiCallTrace trace)
    {
        _chat = chatCompletionService;
        _logger = logger;
        _trace = trace;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WrongQuestionInsightDto>> AnalyzeWrongQuestionsAsync(
        IReadOnlyList<WrongQuestionInsightDto> wrongItems,
        CancellationToken cancellationToken = default)
    {
        if (wrongItems.Count == 0)
            return Array.Empty<WrongQuestionInsightDto>();

        _logger.LogInformation("AI 错题解析（Ark 批量）：Count={Count}", wrongItems.Count);

        try
        {
            return await AnalyzeBatchWithArkAsync(wrongItems, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ark 错题解析失败，回退本地模板");
            _trace.Set("tutor:local-fallback", false);
            return FallbackToLocalAnalysis(wrongItems);
        }
    }

    private async Task<IReadOnlyList<WrongQuestionInsightDto>> AnalyzeBatchWithArkAsync(
        IReadOnlyList<WrongQuestionInsightDto> wrongItems,
        CancellationToken cancellationToken)
    {
        var payload = wrongItems
            .Select(w => new
            {
                w.QuestionId,
                Type = w.Type.ToString(),
                w.StemSummary,
                w.UserAnswer,
                w.StandardAnswer
            })
            .ToList();

        var payloadJson = JsonSerializer.Serialize(payload, ArkChatJsonDefaults.ModelPayloadOptions);

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "system",
                Content =
                    "你是专业教学助手。必须只输出一个 JSON 数组，不要 Markdown 围栏，不要任何说明文字。" +
                    "数组每一项对象字段：QuestionId(number)、RootCause(string)、SolutionHints(string)。" +
                    "须覆盖输入中的全部 QuestionId。"
            },
            new ChatMessage
            {
                Role = "user",
                Content = "错题列表（JSON）：\n" + payloadJson
            }
        };

        var response = await _chat.GenerateCompletionAsync(messages, null, cancellationToken).ConfigureAwait(false);
        var raw = ArkAssistantReply.GetPrimaryText(response);
        var json = ArkModelOutputParsing.ExtractFirstJsonValue(raw);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("模型返回内容为空。");

        var parsed = JsonSerializer.Deserialize<List<TutorAiItemDto>>(json, ArkChatJsonDefaults.ModelPayloadOptions);
        if (parsed == null || parsed.Count == 0)
            throw new InvalidOperationException("无法解析为 JSON 数组。");

        var byId = parsed.ToDictionary(x => x.QuestionId, x => x);
        var results = new List<WrongQuestionInsightDto>(wrongItems.Count);
        var anyLocalFill = false;
        foreach (var item in wrongItems)
        {
            if (byId.TryGetValue(item.QuestionId, out var ai))
            {
                results.Add(new WrongQuestionInsightDto
                {
                    QuestionId = item.QuestionId,
                    Type = item.Type,
                    StemSummary = item.StemSummary,
                    UserAnswer = item.UserAnswer,
                    StandardAnswer = item.StandardAnswer,
                    RootCause = string.IsNullOrWhiteSpace(ai.RootCause) ? "（未给出原因）" : ai.RootCause!,
                    SolutionHints = string.IsNullOrWhiteSpace(ai.SolutionHints) ? "（未给出思路）" : ai.SolutionHints!
                });
            }
            else
            {
                anyLocalFill = true;
                results.Add(LocalTemplateInsight(item, LocalInsightKind.ModelMissedQuestion));
            }
        }

        // 能执行到这里说明已向方舟发起请求且得到 2xx；部分题目本地补全仍算「已调用 API」。
        _trace.Set(anyLocalFill ? "tutor:ark+local-fill" : "tutor:ark", true);
        return results.AsReadOnly();
    }

    private enum LocalInsightKind
    {
        FullFallback,
        ModelMissedQuestion
    }

    private static WrongQuestionInsightDto LocalTemplateInsight(WrongQuestionInsightDto item, LocalInsightKind kind)
    {
        var prefix = kind == LocalInsightKind.FullFallback
            ? "[本地回退·未调用方舟] "
            : "[本地补全·模型未返回本题] ";

        var root = item.Type switch
        {
            QuestionType.SingleChoice => "单选题常见错误来自概念混淆或审题不清。",
            QuestionType.MultipleChoice => "多选题需要检查是否漏选或多选，建议逐项排除。",
            QuestionType.TrueFalse => "判断题建议回到定义与边界条件，避免绝对化表述误判。",
            QuestionType.ShortAnswer => "简答题需要抓住关键词，建议先列提纲再组织语言。",
            QuestionType.FillInBlank => "填空题应对照判分用的正则表达式，检查是否命中应出现的关键词或格式。",
            _ => "该题需要结合知识点复盘。"
        };

        var hints =
            $"建议步骤：1) 回顾知识点标签；2) 对照标准答案「{item.StandardAnswer}」；3) 用自己的话复述结论。";

        return new WrongQuestionInsightDto
        {
            QuestionId = item.QuestionId,
            Type = item.Type,
            StemSummary = item.StemSummary,
            UserAnswer = item.UserAnswer,
            StandardAnswer = item.StandardAnswer,
            RootCause = prefix + root,
            SolutionHints = hints
        };
    }

    private IReadOnlyList<WrongQuestionInsightDto> FallbackToLocalAnalysis(IReadOnlyList<WrongQuestionInsightDto> wrongItems)
    {
        return wrongItems.Select(x => LocalTemplateInsight(x, LocalInsightKind.FullFallback)).ToList().AsReadOnly();
    }

    private sealed class TutorAiItemDto
    {
        public long QuestionId { get; set; }
        public string? RootCause { get; set; }
        public string? SolutionHints { get; set; }
    }
}
