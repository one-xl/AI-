using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 表示一道错题的 AI 解析结果，用于成绩页展示与导出。
/// </summary>
public sealed class WrongQuestionInsightDto
{
    /// <summary>
    /// 获取或设置题目主键。
    /// </summary>
    public long QuestionId { get; init; }

    /// <summary>
    /// 获取或设置题型。
    /// </summary>
    public QuestionType Type { get; init; }

    /// <summary>
    /// 获取或设置题干摘要（可截断）。
    /// </summary>
    public string StemSummary { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置用户作答。
    /// </summary>
    public string UserAnswer { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置标准答案。
    /// </summary>
    public string StandardAnswer { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置错误原因分析（自然语言）。
    /// </summary>
    public string RootCause { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置解题思路（分步建议）。
    /// </summary>
    public string SolutionHints { get; init; } = string.Empty;
}

/// <summary>
/// 表示 AI 题目推荐返回的候选题集合。
/// </summary>
public sealed class QuestionRecommendationDto
{
    /// <summary>
    /// 获取或设置推荐理由。
    /// </summary>
    public string Rationale { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置推荐题目 Id 列表（来自题库）。
    /// </summary>
    public IReadOnlyList<long> RecommendedQuestionIds { get; init; } = Array.Empty<long>();
}

/// <summary>
/// 表示阶段性刷题计划（日程级），供学习计划页展示。
/// </summary>
public sealed class StudyPlanDto
{
    /// <summary>
    /// 获取或设置计划标题。
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置每日建议题量。
    /// </summary>
    public int DailyQuestionQuota { get; init; }

    /// <summary>
    /// 获取或设置重点知识点标签集合。
    /// </summary>
    public IReadOnlyList<string> FocusKnowledgeTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置阶段天数。
    /// </summary>
    public int PhaseDays { get; init; }

    /// <summary>
    /// 获取或设置计划说明（自然语言）。
    /// </summary>
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// 用于生成 AI 计划的输入摘要，聚合用户整体表现。
/// </summary>
public sealed class UserPerformanceSummary
{
    /// <summary>
    /// 获取或设置用户 Id。
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// 获取或设置总答题次数。
    /// </summary>
    public int TotalAttempts { get; init; }

    /// <summary>
    /// 获取或设置正确次数。
    /// </summary>
    public int CorrectAttempts { get; init; }

    /// <summary>
    /// 获取或设置错题条目数。
    /// </summary>
    public int WrongBookCount { get; init; }

    /// <summary>
    /// 获取或设置高频错误知识点标签（逗号分隔或列表）。
    /// </summary>
    public IReadOnlyList<string> WeakTags { get; init; } = Array.Empty<string>();
}
