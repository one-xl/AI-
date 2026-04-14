using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 题库 AI 出题时的附加约束（用于「新建题目」向导式单题生成等场景）。
/// </summary>
public sealed class QuestionBankGenerationHints
{
    /// <summary>
    /// 若指定，则校验模型返回的每条题目的 <c>Difficulty</c> 必须与此枚举值一致。
    /// </summary>
    public DifficultyLevel? RequiredDifficulty { get; init; }

    /// <summary>
    /// 写入用户提示：要求 <c>TopicTags</c> 与这些分类要点一致（可为空）。
    /// </summary>
    public string? TopicTagsHint { get; init; }

    /// <summary>
    /// 写入用户提示：要求题干与 <c>TopicKeywords</c> 贴近这些关键词（可为空）。
    /// </summary>
    public string? TopicKeywordsHint { get; init; }
}
