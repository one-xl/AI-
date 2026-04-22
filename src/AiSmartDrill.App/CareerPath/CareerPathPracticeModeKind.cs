namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// CareerPath 技能包约定的刷题入口模式（与 JSON <c>practice_mode</c> 及 CLI <c>--mode</c> 对应）。
/// </summary>
public enum CareerPathPracticeModeKind
{
    /// <summary>
    /// 按 <c>skills</c> 在题库中筛选后直接开考。
    /// </summary>
    Direct,

    /// <summary>
    /// 先走 AI 题目推荐，再开考。
    /// </summary>
    AiRecommend
}
