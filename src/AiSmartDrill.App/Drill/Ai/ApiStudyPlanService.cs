using System.Text.Json;
using AiSmartDrill.App.Drill.Ai.Ark;
using AiSmartDrill.App.Drill.Ai.Client;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 基于火山方舟 Chat Completions 的学习计划生成：解析 JSON 计划 DTO，失败时回退本地启发式计划。
/// </summary>
public sealed class ApiStudyPlanService : IStudyPlanService
{
    private readonly IChatCompletionService _chat;
    private readonly ILogger<ApiStudyPlanService> _logger;
    private readonly AiCallTrace _trace;

    /// <summary>
    /// 初始化 <see cref="ApiStudyPlanService"/>。
    /// </summary>
    public ApiStudyPlanService(
        IChatCompletionService chatCompletionService,
        ILogger<ApiStudyPlanService> logger,
        AiCallTrace trace)
    {
        _chat = chatCompletionService;
        _logger = logger;
        _trace = trace;
    }

    /// <inheritdoc />
    public async Task<StudyPlanDto> GeneratePlanAsync(UserPerformanceSummary summary, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI 学习计划（Ark）：UserId={UserId}", summary.UserId);

        try
        {
            var plan = await CallArkAsync(summary, cancellationToken).ConfigureAwait(false);
            _trace.Set("plan:ark", true);
            return plan;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ark 学习计划失败，回退本地");
            _trace.Set("plan:local-fallback", false);
            return FallbackLocalPlan(summary);
        }
    }

    private async Task<StudyPlanDto> CallArkAsync(UserPerformanceSummary summary, CancellationToken cancellationToken)
    {
        var weakKp = string.Join(", ", summary.WeakKnowledgePoints);
        var weakTopics = string.Join(", ", summary.WeakTopicTags);

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "system",
                Content =
                    "你是学习规划助手。只输出一个 JSON 对象，不要 Markdown，不要解释。\n" +
                    "字段：Title(string)、DailyQuestionQuota(number)、FocusKnowledgeTags(string[])、FocusKnowledgePoints(string[])、PhaseDays(number)、Notes(string)。\n" +
                    "FocusKnowledgeTags 表示需要加强的模块/大标签（对应题库 TopicTags 粒度）。\n" +
                    "FocusKnowledgePoints 表示需要补学的细粒度知识点（可对照教材的小节/考点，至少 3 条、不超过 10 条）。\n" +
                    "Notes 不超过 80 字。"
            },
            new ChatMessage
            {
                Role = "user",
                Content =
                    $"生成计划：用户ID={summary.UserId}, 总答题={summary.TotalAttempts}, 正确={summary.CorrectAttempts}, 错题条目={summary.WrongBookCount}, 弱项模块标签={weakTopics}, 弱项细知识点={weakKp}"
            }
        };

        var response = await _chat
            .GenerateCompletionAsync(messages, null, AiCompletionTokenBudgets.StudyPlanJson, cancellationToken)
            .ConfigureAwait(false);
        var raw = ArkAssistantReply.GetPrimaryText(response);
        var json = ArkModelOutputParsing.ExtractFirstJsonValue(raw);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("模型返回为空。");

        var plan = JsonSerializer.Deserialize<StudyPlanDto>(json, ArkChatJsonDefaults.ModelPayloadOptions)
                   ?? throw new InvalidOperationException("学习计划 JSON 反序列化失败。");

        if (plan.FocusKnowledgePoints.Count == 0 && summary.WeakKnowledgePoints.Count > 0)
        {
            return new StudyPlanDto
            {
                Title = plan.Title,
                DailyQuestionQuota = plan.DailyQuestionQuota,
                FocusKnowledgeTags = plan.FocusKnowledgeTags,
                FocusKnowledgePoints = summary.WeakKnowledgePoints,
                PhaseDays = plan.PhaseDays,
                Notes = plan.Notes
            };
        }

        return plan;
    }

    private static StudyPlanDto FallbackLocalPlan(UserPerformanceSummary summary)
    {
        var dailyQuota = summary.TotalAttempts > 0 ? Math.Max(5, summary.TotalAttempts / 10) : 5;
        var phaseDays = summary.WrongBookCount > 10 ? 14 : 7;

        var moduleTags = summary.WeakTopicTags.Count > 0
            ? summary.WeakTopicTags
            : new List<string> { "基础巩固", "错题复盘", "限时训练" };

        var finePoints = summary.WeakKnowledgePoints.Count > 0
            ? summary.WeakKnowledgePoints
            : new List<string> { "核心概念复盘", "典型题再练", "易错点对照" };

        return new StudyPlanDto
        {
            Title = "[本地回退] 个性化学习计划",
            DailyQuestionQuota = dailyQuota,
            FocusKnowledgeTags = moduleTags,
            FocusKnowledgePoints = finePoints,
            PhaseDays = phaseDays,
            Notes = "[本地回退·未调用方舟] 基于本地规则生成的计划；若方舟可用，此处应由模型生成。"
        };
    }
}
