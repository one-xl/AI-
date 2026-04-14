using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 描述一次「AI 按模板生成题目」的聚合结果：成功入库候选、校验失败原因与模型原文片段（截断）。
/// </summary>
public sealed class QuestionBankAiGenerationResult
{
    /// <summary>
    /// 初始化 <see cref="QuestionBankAiGenerationResult"/>。
    /// </summary>
    /// <param name="questions">已通过校验并设置领域的题目列表。</param>
    /// <param name="errors">逐条校验失败说明。</param>
    /// <param name="rawSnippet">模型原始输出截断片段，便于排错。</param>
    public QuestionBankAiGenerationResult(
        IReadOnlyList<Question> questions,
        IReadOnlyList<string> errors,
        string rawSnippet)
    {
        Questions = questions;
        Errors = errors;
        RawSnippet = rawSnippet;
    }

    /// <summary>
    /// 获取可直接 <c>AddRange</c> 的题目集合。
    /// </summary>
    public IReadOnlyList<Question> Questions { get; }

    /// <summary>
    /// 获取解析或字段校验失败时的错误行（中文描述）。
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// 获取模型输出截断（避免日志与界面被过长文本淹没）。
    /// </summary>
    public string RawSnippet { get; }
}
