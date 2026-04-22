using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 题库 AI 出题时的附加约束（单题向导或批量入库）。<see cref="RequiredDifficulty"/> 与
/// <see cref="RandomizeDifficultyInBatch"/> 不得同时为真语义；后者为 true 时服务端忽略锁定难度。
/// </summary>
public sealed class QuestionBankGenerationHints
{
    /// <summary>
    /// 若指定且未启用 <see cref="RandomizeDifficultyInBatch"/>，则校验模型返回的每条题目的 <c>Difficulty</c> 必须与此枚举值一致。
    /// </summary>
    public DifficultyLevel? RequiredDifficulty { get; init; }

    /// <summary>
    /// 为 true 时：本批每条题目须在 Easy / Medium / Hard 间随机取值并尽量均衡，不校验 <see cref="RequiredDifficulty"/>。
    /// </summary>
    public bool RandomizeDifficultyInBatch { get; init; }

    /// <summary>
    /// 写入用户提示：要求 <c>KnowledgeTags</c> / <c>PrimaryKnowledgePoint</c> 围绕这些知识点短语展开（可为空）。
    /// </summary>
    public string? KnowledgeTagsHint { get; init; }

    /// <summary>
    /// 写入用户提示：要求 <c>TopicTags</c> 与这些分类要点一致（可为空）。
    /// </summary>
    public string? TopicTagsHint { get; init; }

    /// <summary>
    /// 写入用户提示：要求题干与 <c>TopicKeywords</c> 贴近这些关键词（可为空）。
    /// </summary>
    public string? TopicKeywordsHint { get; init; }
}
