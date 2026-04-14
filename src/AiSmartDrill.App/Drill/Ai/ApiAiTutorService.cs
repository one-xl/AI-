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
        catch (OperationCanceledException)
        {
            throw;
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
                StemFull = string.IsNullOrWhiteSpace(w.StemFull) ? w.StemSummary : w.StemFull,
                w.OptionsJson,
                w.KnowledgeTags,
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
                    "数组每一项对象字段：QuestionId(number)、RootCause(string)、SolutionHints(string)、OptionAnalysis(string)。" +
                    "须覆盖输入中的全部 QuestionId；按输入 Type 决定详略，禁止对所有题型一刀切短写。\n" +
                    "【RootCause】2～4 句中文：考查点 + 对照用户作答与标准答案的具体错因；禁止空泛套话单独成段。\n" +
                    "【SolutionHints】3～5 条短句：可操作的改正步骤（审题、关键定义/公式、易错点、自检）。\n" +
                    "【OptionAnalysis】须与 Type、OptionsJson、UserAnswer、StandardAnswer 一致：\n" +
                    "- 若 Type 为 SingleChoice 或 MultipleChoice，且 OptionsJson 为合法 JSON 字符串数组（至少 2 项）：**逐选项写细解析**。" +
                    "按 A、B、C… 与数组顺序一一对应；每个选项单独一小段，段首写「A:」「B:」等，**每选项 2～4 句**：选项文字本身对错、与题干条件/知识点的关系、常见误选原因；" +
                    "若该选项是标答组成部分或用户误选/漏选，必须点明；**所有选项写完后**另起一段「小结：」用 2～3 句汇总标答、用户作答、错因（错选/漏选/多选哪些字母）。\n" +
                    "- 若为 TrueFalse：共 4～8 句即可，直接写对错判定依据、关键定义边界、用户错因，不要虚构 A/B 选项段。\n" +
                    "- 若为 ShortAnswer、FillInBlank，或 OptionsJson 为空/非选项数组：共 5～10 句**直接解析**，写清得分关键词或正则判分要点、用户答案偏差、应如何改写成可得分表述；不要逐字母选项。\n" +
                    "解析一律以 StemFull 为准，StemSummary 仅作参考。"
            },
            new ChatMessage
            {
                Role = "user",
                Content = "错题列表（JSON，含完整题干 StemFull 与 OptionsJson）：\n" + payloadJson
            }
        };

        var response = await _chat
            .GenerateCompletionAsync(messages, null, AiCompletionTokenBudgets.TutorWrongBatch(wrongItems.Count), cancellationToken)
            .ConfigureAwait(false);
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
                var opt = string.IsNullOrWhiteSpace(ai.OptionAnalysis)
                    ? WrongQuestionInsightTextFallback.BuildOptionAnalysis(item)
                    : ai.OptionAnalysis!.Trim();
                results.Add(new WrongQuestionInsightDto
                {
                    QuestionId = item.QuestionId,
                    Type = item.Type,
                    StemSummary = item.StemSummary,
                    StemFull = item.StemFull,
                    OptionsJson = item.OptionsJson,
                    KnowledgeTags = item.KnowledgeTags,
                    UserAnswer = item.UserAnswer,
                    StandardAnswer = item.StandardAnswer,
                    RootCause = string.IsNullOrWhiteSpace(ai.RootCause) ? "（未给出原因）" : ai.RootCause!,
                    SolutionHints = string.IsNullOrWhiteSpace(ai.SolutionHints) ? "（未给出思路）" : ai.SolutionHints!,
                    OptionAnalysis = opt
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
            StemFull = item.StemFull,
            OptionsJson = item.OptionsJson,
            KnowledgeTags = item.KnowledgeTags,
            UserAnswer = item.UserAnswer,
            StandardAnswer = item.StandardAnswer,
            RootCause = prefix + root,
            SolutionHints = hints,
            OptionAnalysis = WrongQuestionInsightTextFallback.BuildOptionAnalysis(item)
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
        public string? OptionAnalysis { get; set; }
    }
}
