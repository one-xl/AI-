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

        var plan = new StudyPlanDto
        {
            Title = "个性化阶段性刷题计划（演示）",
            DailyQuestionQuota = daily,
            FocusKnowledgeTags = moduleTags,
            FocusKnowledgePoints = finePoints,
            PhaseDays = days,
            Notes =
                $"当前估算正确率：{accuracy:P0}。建议每天完成 {daily} 题，并优先处理错题本中 {summary.WrongBookCount} 个薄弱点。"
        };

        return Task.FromResult(plan);
    }
}
