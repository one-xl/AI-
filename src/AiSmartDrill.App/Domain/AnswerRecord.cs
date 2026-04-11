namespace AiSmartDrill.App.Domain;

/// <summary>
/// 表示一次用户作答记录，映射到核心表“用户答题记录表”。
/// </summary>
public sealed class AnswerRecord
{
    /// <summary>
    /// 获取或设置记录主键。
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
    /// 获取或设置会话/试卷标识，用于同一次考试聚合。
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 获取或设置用户作答内容。
    /// </summary>
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置是否正确。
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// 获取或设置本题得分（演示版通常为 0 或 1）。
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 获取或设置作答耗时（毫秒）。
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// 获取或设置记录时间（UTC）。
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// 获取或设置关联用户。
    /// </summary>
    public AppUser? User { get; set; }

    /// <summary>
    /// 获取或设置关联题目。
    /// </summary>
    public Question? Question { get; set; }
}
