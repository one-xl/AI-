namespace AiSmartDrill.App.Domain;

/// <summary>
/// 表示题目难度等级，用于题库筛选与随机组卷时的分层抽样。
/// </summary>
public enum DifficultyLevel
{
    /// <summary>
    /// 入门难度，适合建立信心与熟悉界面。
    /// </summary>
    Easy = 0,

    /// <summary>
    /// 中等难度，覆盖大部分考试场景。
    /// </summary>
    Medium = 1,

    /// <summary>
    /// 困难难度，用于拔高训练与错题巩固。
    /// </summary>
    Hard = 2
}
