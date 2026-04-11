namespace AiSmartDrill.App.Domain;

/// <summary>
/// 表示错题本中的一条聚合记录，映射到核心表“错题本表”。
/// 同一用户同一题目在业务上应保持唯一（由数据库唯一索引保障）。
/// </summary>
public sealed class WrongBookEntry
{
    /// <summary>
    /// 获取或设置主键。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置用户外键。
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 获取或设置题目外键。
    /// </summary>
    public long QuestionId { get; set; }

    /// <summary>
    /// 获取或设置累计错误次数。
    /// </summary>
    public int WrongCount { get; set; }

    /// <summary>
    /// 获取或设置最近一次错误时间（UTC）。
    /// </summary>
    public DateTime LastWrongAtUtc { get; set; }

    /// <summary>
    /// 获取或设置关联用户。
    /// </summary>
    public AppUser? User { get; set; }

    /// <summary>
    /// 获取或设置关联题目。
    /// </summary>
    public Question? Question { get; set; }
}
