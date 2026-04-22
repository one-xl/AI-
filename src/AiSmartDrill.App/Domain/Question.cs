namespace AiSmartDrill.App.Domain;

/// <summary>
/// 表示题库中的一道题目，映射到核心表“题库表”。
/// </summary>
public sealed class Question
{
    /// <summary>
    /// 获取或设置题目主键。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置题型。
    /// </summary>
    public QuestionType Type { get; set; }

    /// <summary>
    /// 获取或设置难度。
    /// </summary>
    public DifficultyLevel Difficulty { get; set; }

    /// <summary>
    /// 获取或设置题目领域/学科。
    /// </summary>
    public QuestionDomain Domain { get; set; }

    /// <summary>
    /// 获取或设置题干文本。
    /// </summary>
    public string Stem { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置标准答案（客观题存键值，主观题存参考答案）。
    /// </summary>
    public string StandardAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置选项 JSON（客观题使用），为空表示非客观题或选项内嵌题干。
    /// </summary>
    public string? OptionsJson { get; set; }

    /// <summary>
    /// 获取或设置细粒度知识点标签（逗号分隔）：可教、可测的考点短语；领域以 <see cref="Domain"/> 为准，勿在此重复填领域名。
    /// </summary>
    public string KnowledgeTags { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置本题主知识点（应与 <see cref="KnowledgeTags"/> 中某一项一致），供推荐与统计做严格匹配。
    /// </summary>
    public string PrimaryKnowledgePoint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置领域内的模块/大标签（逗号/分号分隔），用于章节级筛选与学习计划中的模块弱项。
    /// </summary>
    public string TopicTags { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置检索关键词（逗号/分号分隔），可与题干一起参与推荐匹配。
    /// </summary>
    public string TopicKeywords { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置是否启用（软删除/下架）。
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 获取或设置创建时间（UTC）。
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// 获取导航集合：该题的所有答题记录。
    /// </summary>
    public ICollection<AnswerRecord> AnswerRecords { get; set; } = new List<AnswerRecord>();

    /// <summary>
    /// 获取导航集合：该题关联的错题本条目。
    /// </summary>
    public ICollection<WrongBookEntry> WrongBookEntries { get; set; } = new List<WrongBookEntry>();
}
