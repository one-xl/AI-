namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// AI 学习计划生成服务契约：输入统计摘要，输出阶段性刷题计划。
/// </summary>
public interface IStudyPlanService
{
    /// <summary>
    /// 异步生成学习计划。
    /// </summary>
    /// <param name="summary">用户表现摘要。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>学习计划 DTO。</returns>
    Task<StudyPlanDto> GeneratePlanAsync(UserPerformanceSummary summary, CancellationToken cancellationToken = default);
}
