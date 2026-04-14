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
        var weakTagsString = string.Join(", ", summary.WeakTags);

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "system",
                Content =
                    "你是学习规划助手。只输出一个 JSON 对象，不要 Markdown，不要解释。字段：Title(string)、DailyQuestionQuota(number)、FocusKnowledgeTags(string[])、PhaseDays(number)、Notes(string)。Notes 不超过 60 字。"
            },
            new ChatMessage
            {
                Role = "user",
                Content =
                    $"生成计划：用户ID={summary.UserId}, 总答题={summary.TotalAttempts}, 正确={summary.CorrectAttempts}, 错题条目={summary.WrongBookCount}, 弱项={weakTagsString}"
            }
        };

        var response = await _chat
            .GenerateCompletionAsync(messages, null, AiCompletionTokenBudgets.StudyPlanJson, cancellationToken)
            .ConfigureAwait(false);
        var raw = ArkAssistantReply.GetPrimaryText(response);
        var json = ArkModelOutputParsing.ExtractFirstJsonValue(raw);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("模型返回为空。");

        var plan = JsonSerializer.Deserialize<StudyPlanDto>(json, ArkChatJsonDefaults.ModelPayloadOptions);
        return plan ?? throw new InvalidOperationException("学习计划 JSON 反序列化失败。");
    }

    private static StudyPlanDto FallbackLocalPlan(UserPerformanceSummary summary)
    {
        var dailyQuota = summary.TotalAttempts > 0 ? Math.Max(5, summary.TotalAttempts / 10) : 5;
        var phaseDays = summary.WrongBookCount > 10 ? 14 : 7;

        return new StudyPlanDto
        {
            Title = "[本地回退] 个性化学习计划",
            DailyQuestionQuota = dailyQuota,
            FocusKnowledgeTags = summary.WeakTags.Count > 0 ? summary.WeakTags : new List<string> { "基础知识" },
            PhaseDays = phaseDays,
            Notes = "[本地回退·未调用方舟] 基于本地规则生成的计划；若方舟可用，此处应由模型生成。"
        };
    }
}
