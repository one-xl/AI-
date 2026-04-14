using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 考试/刷题侧：将当前题目上下文发送给模型，返回面向学习者的中文解析文本。
/// </summary>
public interface IExamQuestionAiExplainService
{
    /// <summary>
    /// 基于题干、选项与用户当前作答（可为空）生成讲解。
    /// </summary>
    /// <param name="question">当前题库题目实体。</param>
    /// <param name="userAnswerSnapshot">用户当前输入或按钮选择快照。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>结论与详解两段（模型须按提示先写【结论】再写【详解】）。</returns>
    Task<ExamQuestionExplainResult> ExplainQuestionAsync(
        Question question,
        string? userAnswerSnapshot,
        CancellationToken cancellationToken = default);
}
