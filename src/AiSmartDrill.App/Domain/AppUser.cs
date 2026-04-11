namespace AiSmartDrill.App.Domain;

/// <summary>
/// 表示系统中的学习者用户，用于外键关联答题记录与错题本。
/// 演示版默认使用单用户（Id=1），后续可扩展登录模块。
/// </summary>
public sealed class AppUser
{
    /// <summary>
    /// 获取或设置用户主键。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置显示名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置创建时间（UTC）。
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}
