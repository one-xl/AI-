using System.Linq;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 本地占位实现的学习计划服务：用可解释规则把摘要映射为日程建议。
/// </summary>
public sealed class LocalStudyPlanService : IStudyPlanService
{
    private readonly ILogger<LocalStudyPlanService> _logger;

    /// <summary>
    /// 初始化 <see cref="LocalStudyPlanService"/> 的新实例。
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    public LocalStudyPlanService(ILogger<LocalStudyPlanService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<StudyPlanDto> GeneratePlanAsync(UserPerformanceSummary summary, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI 计划生成（占位）：UserId={UserId}", summary.UserId);

        // 正确率越低，每日题量建议越高（在合理范围内），体现“补短板”策略。
        var accuracy = summary.TotalAttempts <= 0
            ? 0.65
            : (double)summary.CorrectAttempts / summary.TotalAttempts;

        var daily = (int)Math.Clamp(Math.Round(12 + (1.0 - accuracy) * 20), 8, 28);
        var days = summary.WrongBookCount >= 8 ? 14 : 7;

        var moduleTags = summary.WeakTopicTags.Count > 0
            ? summary.WeakTopicTags
            : new[] { "基础巩固", "错题复盘", "限时训练" }.ToList();

        var finePoints = summary.WeakKnowledgePoints.Count > 0
            ? summary.WeakKnowledgePoints
            : new[] { "核心概念复盘", "典型题再练", "易错点对照" }.ToList();

        var stages = new[]
        {
            new StudyPlanStageDto
            {
                StageName = "基础修复",
                DayRange = $"第 1-{Math.Max(2, days / 3)} 天",
                DailyNewQuestionQuota = Math.Max(4, daily - 4),
                DailyReviewQuestionQuota = 4,
                FocusKnowledgeTags = moduleTags.Take(2).ToList(),
                FocusKnowledgePoints = finePoints.Take(2).ToList(),
                Goal = "先纠正高频概念错误。",
                Checklist = new[]
                {
                    $"完成 {Math.Max(4, daily - 4)} 道基础题",
                    "复盘当天错题与错因",
                    "整理核心概念卡片"
                }
            },
            new StudyPlanStageDto
            {
                StageName = "强化提升",
                DayRange = $"第 {Math.Max(2, days / 3) + 1}-{Math.Max(4, days - 2)} 天",
                DailyNewQuestionQuota = Math.Max(5, daily - 3),
                DailyReviewQuestionQuota = 5,
                FocusKnowledgeTags = moduleTags.Skip(1).Take(2).DefaultIfEmpty(moduleTags.First()).ToList(),
                FocusKnowledgePoints = finePoints.Skip(1).Take(3).DefaultIfEmpty(finePoints.First()).ToList(),
                Goal = "提高综合题稳定性和速度。",
                Checklist = new[]
                {
                    "按模块做综合题",
                    "重做前两天错题",
                    "记录重复失分点"
                }
            },
            new StudyPlanStageDto
            {
                StageName = "冲刺复盘",
                DayRange = $"第 {Math.Max(5, days - 1)}-{days} 天",
                DailyNewQuestionQuota = Math.Max(3, daily - 5),
                DailyReviewQuestionQuota = 6,
                FocusKnowledgeTags = moduleTags.Take(2).ToList(),
                FocusKnowledgePoints = finePoints.TakeLast(Math.Min(3, finePoints.Count)).ToList(),
                Goal = "稳定正确率并减少重复错误。",
                Checklist = new[]
                {
                    "每日完成一轮限时小测",
                    "复盘仍不稳定的知识点",
                    "按耗时和正确率调整节奏"
                }
            }
        };

        var plan = new StudyPlanDto
        {
            Theme = moduleTags.FirstOrDefault() ?? "学习强化",
            Title = "个性化阶段性刷题计划（演示）",
            DailyQuestionQuota = daily,
            FocusKnowledgeTags = moduleTags,
            FocusKnowledgePoints = finePoints,
            PhaseDays = days,
            TotalQuestionQuota = daily * days,
            StagePlans = stages,
            Notes =
                $"当前估算正确率：{accuracy:P0}。建议每天完成 {daily} 题，并优先处理错题本中 {summary.WrongBookCount} 个薄弱点。"
        };

        return Task.FromResult(plan);
    }
}
