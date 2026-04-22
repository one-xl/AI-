namespace AiSmartDrill.App.Domain;

/// <summary>
/// 表示一次已保存的学习计划快照，供历史记录回看与复用。
/// </summary>
public sealed class StudyPlanHistoryEntry
{
    /// <summary>
    /// 获取或设置历史记录主键。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置用户主键。
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 获取或设置计划主题。
    /// </summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置计划标题。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置已格式化的计划正文。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置保存时间（UTC）。
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// 获取本地时区显示时间，供界面历史列表展示。
    /// </summary>
    public DateTime CreatedAtLocalTime => CreatedAtUtc.ToLocalTime();

    /// <summary>
    /// 获取或设置关联用户。
    /// </summary>
    public AppUser? User { get; set; }
}
