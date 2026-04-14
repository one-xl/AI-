namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// AI 题目推荐服务契约：依据历史正确率、错误类型与知识点推荐候选题。
/// </summary>
public interface IQuestionRecommendationService
{
    /// <summary>
    /// 异步生成推荐题目列表（返回题库中已有题目的 Id）。
    /// </summary>
    /// <param name="userId">用户 Id。</param>
    /// <param name="request">可选：错题勾选、领域范围；为 null 时使用默认（全领域、无勾选信号）。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>推荐结果 DTO。</returns>
    Task<QuestionRecommendationDto> RecommendAsync(
        long userId,
        QuestionRecommendationRequest? request = null,
        CancellationToken cancellationToken = default);
}
