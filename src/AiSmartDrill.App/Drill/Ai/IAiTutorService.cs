namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// AI 错题解析服务契约：交卷后批量分析错题上下文并返回结构化结果。
/// 真实环境可替换为 HTTP/gRPC 客户端实现。
/// </summary>
public interface IAiTutorService
{
    /// <summary>
    /// 异步分析错题列表并返回解析结果。
    /// </summary>
    /// <param name="wrongItems">错题输入集合。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>结构化解析结果集合。</returns>
    Task<IReadOnlyList<WrongQuestionInsightDto>> AnalyzeWrongQuestionsAsync(
        IReadOnlyList<WrongQuestionInsightDto> wrongItems,
        CancellationToken cancellationToken = default);
}
