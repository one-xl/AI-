using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 题库管理侧：按指定领域与题型模板调用大模型，生成符合导入契约的 JSON 题目列表。
/// </summary>
public interface IQuestionBankAiGenerationService
{
    /// <summary>
    /// 调用模型生成题目，并解析为可直接入库的 <see cref="Question"/> 实体（领域由调用方预先写入）。
    /// </summary>
    /// <param name="domain">题目领域枚举值（与 UI 领域下拉一致）。</param>
    /// <param name="domainDisplayName">领域中文显示名，写入提示词供模型对齐语境。</param>
    /// <param name="templateType">模板锁定的题型（单选/多选/判断/简答/填空之一）。</param>
    /// <param name="count">希望生成的题目数量（服务内部会裁剪到合理上限）。</param>
    /// <param name="hints">可选约束（难度锁定、分类标签与关键词写作方向等）。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>生成与校验结果（可能包含部分成功与解析错误）。</returns>
    Task<QuestionBankAiGenerationResult> GenerateQuestionsAsync(
        QuestionDomain domain,
        string domainDisplayName,
        QuestionType templateType,
        int count,
        QuestionBankGenerationHints? hints = null,
        CancellationToken cancellationToken = default);
}
