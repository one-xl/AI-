using System.Linq;
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
        var weakKp = FormatWeaknessStats(summary.WeakKnowledgePointStats);
        var weakTopics = FormatWeaknessStats(summary.WeakTopicTagStats);
        var accuracy = summary.TotalAttempts <= 0
            ? 0d
            : (double)summary.CorrectAttempts / summary.TotalAttempts;

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = "system",
                Content =
                    "你是严谨的刷题学习规划助手。只输出一个 JSON 对象，不要 Markdown，不要解释，不要额外文本。\n" +
                    "字段必须完整：Theme(string)、Title(string)、DailyQuestionQuota(number)、FocusKnowledgeTags(string[])、FocusKnowledgePoints(string[])、PhaseDays(number)、TotalQuestionQuota(number)、StagePlans(array)、Notes(string)。\n" +
                    "其中 StagePlans 每项包含：StageName(string)、DayRange(string)、DailyNewQuestionQuota(number)、DailyReviewQuestionQuota(number)、FocusKnowledgeTags(string[])、FocusKnowledgePoints(string[])、Goal(string)、Checklist(string[])。\n" +
                    "要求：\n" +
                    "1. 计划必须具体到每个阶段每天刷多少新题、复盘多少旧题。\n" +
                    "2. FocusKnowledgePoints 必须是细粒度可执行知识点，至少 4 条，不超过 10 条。\n" +
                    "3. StagePlans 至少 3 个阶段，合计覆盖全部天数；题量安排要和用户正确率、错题量、弱项频次匹配。\n" +
                    "4. Checklist 写可执行动作，如“完成 12 道单选并复盘 6 道错题”。\n" +
                    "5. Notes 用 2~4 句说明阶段衔接与节奏控制，不超过 160 字。\n" +
                    "6. Theme 是简洁主题，例如“C# 集合与泛型强化”；Title 是完整计划名。"
            },
            new ChatMessage
            {
                Role = "user",
                Content =
                    "请基于以下学习画像生成详细学习计划：" +
                    $"\n- 用户ID：{summary.UserId}" +
                    $"\n- 总答题数：{summary.TotalAttempts}" +
                    $"\n- 正确题数：{summary.CorrectAttempts}" +
                    $"\n- 当前正确率：{accuracy:P0}" +
                    $"\n- 错题本条目数：{summary.WrongBookCount}" +
                    $"\n- 弱项模块频次：{weakTopics}" +
                    $"\n- 弱项知识点频次：{weakKp}" +
                    "\n请给出分阶段、可执行、题量明确的规划。"
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

        return NormalizePlan(plan, summary);
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

        var stages = BuildFallbackStages(phaseDays, dailyQuota, moduleTags, finePoints);

        return new StudyPlanDto
        {
            Theme = moduleTags.FirstOrDefault() ?? "阶段强化",
            Title = "[本地回退] 个性化学习计划",
            DailyQuestionQuota = dailyQuota,
            FocusKnowledgeTags = moduleTags,
            FocusKnowledgePoints = finePoints,
            PhaseDays = phaseDays,
            TotalQuestionQuota = dailyQuota * phaseDays,
            StagePlans = stages,
            Notes = "[本地回退·未调用方舟] 已按弱项模块、知识点与题量节奏生成分阶段计划；若方舟可用，此处会给出更细致的 AI 规划。"
        };
    }

    private static string FormatWeaknessStats(IReadOnlyList<WeaknessStatDto> stats)
    {
        if (stats.Count == 0)
        {
            return "暂无明显弱项统计";
        }

        return string.Join("；", stats.Select(x => $"{x.Name}({x.Count})"));
    }

    private static StudyPlanDto NormalizePlan(StudyPlanDto plan, UserPerformanceSummary summary)
    {
        var focusTags = plan.FocusKnowledgeTags.Count > 0 ? plan.FocusKnowledgeTags : summary.WeakTopicTags;
        var focusPoints = plan.FocusKnowledgePoints.Count > 0 ? plan.FocusKnowledgePoints : summary.WeakKnowledgePoints;
        var phaseDays = Math.Max(plan.PhaseDays, plan.StagePlans.Count > 0 ? plan.PhaseDays : (summary.WrongBookCount > 10 ? 14 : 7));
        var dailyQuota = Math.Max(6, plan.DailyQuestionQuota > 0 ? plan.DailyQuestionQuota : Math.Max(8, summary.TotalAttempts / 10));
        var stages = plan.StagePlans.Count > 0
            ? plan.StagePlans
            : BuildFallbackStages(phaseDays, dailyQuota, focusTags, focusPoints);
        var totalQuestionQuota = plan.TotalQuestionQuota > 0
            ? plan.TotalQuestionQuota
            : dailyQuota * phaseDays;

        return new StudyPlanDto
        {
            Theme = string.IsNullOrWhiteSpace(plan.Theme) ? (focusTags.FirstOrDefault() ?? "阶段强化") : plan.Theme.Trim(),
            Title = string.IsNullOrWhiteSpace(plan.Title) ? "个性化学习计划" : plan.Title.Trim(),
            DailyQuestionQuota = dailyQuota,
            FocusKnowledgeTags = focusTags,
            FocusKnowledgePoints = focusPoints,
            PhaseDays = phaseDays,
            TotalQuestionQuota = totalQuestionQuota,
            StagePlans = stages,
            Notes = string.IsNullOrWhiteSpace(plan.Notes)
                ? "先完成基础巩固，再进入强化训练，最后做限时复盘。"
                : plan.Notes.Trim()
        };
    }

    private static IReadOnlyList<StudyPlanStageDto> BuildFallbackStages(
        int phaseDays,
        int dailyQuota,
        IReadOnlyList<string> moduleTags,
        IReadOnlyList<string> finePoints)
    {
        var first = Math.Max(2, phaseDays / 3);
        var second = Math.Max(2, phaseDays / 3);

        return new[]
        {
            new StudyPlanStageDto
            {
                StageName = "基础巩固",
                DayRange = $"第 1-{first} 天",
                DailyNewQuestionQuota = Math.Max(4, dailyQuota - 4),
                DailyReviewQuestionQuota = 4,
                FocusKnowledgeTags = moduleTags.Take(2).ToList(),
                FocusKnowledgePoints = finePoints.Take(2).ToList(),
                Goal = "先把高频薄弱点重新建立正确解题路径。",
                Checklist = new[]
                {
                    $"完成 {Math.Max(4, dailyQuota - 4)} 道新题并记录错因",
                    "复盘当天错题与历史错题",
                    "对照教材梳理核心定义与易混点"
                }
            },
            new StudyPlanStageDto
            {
                StageName = "强化训练",
                DayRange = $"第 {first + 1}-{first + second} 天",
                DailyNewQuestionQuota = Math.Max(5, dailyQuota - 3),
                DailyReviewQuestionQuota = 5,
                FocusKnowledgeTags = moduleTags.Skip(1).Take(2).DefaultIfEmpty(moduleTags.FirstOrDefault() ?? "模块强化").ToList(),
                FocusKnowledgePoints = finePoints.Skip(1).Take(3).DefaultIfEmpty(finePoints.FirstOrDefault() ?? "综合应用").ToList(),
                Goal = "把单点知识迁移到综合题和限时训练中。",
                Checklist = new[]
                {
                    $"完成 {Math.Max(5, dailyQuota - 3)} 道综合题",
                    "重做前一阶段错题并比较两次答案差异",
                    "按知识点整理一份个人易错清单"
                }
            },
            new StudyPlanStageDto
            {
                StageName = "冲刺复盘",
                DayRange = $"第 {first + second + 1}-{phaseDays} 天",
                DailyNewQuestionQuota = Math.Max(3, dailyQuota - 5),
                DailyReviewQuestionQuota = 6,
                FocusKnowledgeTags = moduleTags.Take(2).ToList(),
                FocusKnowledgePoints = finePoints.TakeLast(Math.Min(3, finePoints.Count)).ToList(),
                Goal = "稳定正确率，减少重复性失分。",
                Checklist = new[]
                {
                    "每日限时完成一轮小套题",
                    "集中复盘仍会出错的知识点",
                    "按正确率与耗时微调后续刷题节奏"
                }
            }
        };
    }
}
