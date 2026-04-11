using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 本地占位实现的题目推荐服务：基于错题知识点做简单相似度匹配。
/// </summary>
public sealed class LocalQuestionRecommendationService : IQuestionRecommendationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<LocalQuestionRecommendationService> _logger;

    /// <summary>
    /// 初始化 <see cref="LocalQuestionRecommendationService"/> 的新实例。
    /// </summary>
    /// <param name="dbFactory">数据库上下文工厂。</param>
    /// <param name="logger">日志记录器。</param>
    public LocalQuestionRecommendationService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<LocalQuestionRecommendationService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<QuestionRecommendationDto> RecommendAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // 取出错题关联题目的知识点标签，作为推荐的弱项信号。
        var wrongTags = await db.WrongBookEntries
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (_, q) => q)
            .Select(q => q.KnowledgeTags)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tags in wrongTags)
        {
            foreach (var t in tags.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                tagSet.Add(t);
            }
        }

        // 从题库中挑选：同标签命中且非已禁用，最多 8 题。
        var all = await db.Questions.AsNoTracking()
            .Where(q => q.IsEnabled)
            .OrderByDescending(q => q.Id)
            .Take(200)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var scored = all
            .Select(q => new
            {
                q.Id,
                Score = tagSet.Count == 0
                    ? 1
                    : q.KnowledgeTags.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                        .Count(t => tagSet.Contains(t.Trim()))
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Id)
            .Take(8)
            .Select(x => x.Id)
            .ToList();

        _logger.LogInformation("AI 题目推荐（占位）：UserId={UserId}, Picked={Count}", userId, scored.Count);

        return new QuestionRecommendationDto
        {
            Rationale = tagSet.Count == 0
                ? "暂无错题标签信号：推荐最近入库的题目用于巩固。"
                : "基于错题知识点标签，从题库中挑选最相近的候选题。",
            RecommendedQuestionIds = scored
        };
    }
}
