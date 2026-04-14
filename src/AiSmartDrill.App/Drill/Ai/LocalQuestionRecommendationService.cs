using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 本地占位实现的题目推荐：与云端版一致的排除错题、领域范围与标签/关键词筛选逻辑。
/// </summary>
public sealed class LocalQuestionRecommendationService : IQuestionRecommendationService
{
    private const int TargetCount = 8;

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
    public async Task<QuestionRecommendationDto> RecommendAsync(
        long userId,
        QuestionRecommendationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        request ??= new QuestionRecommendationRequest();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var wrongQuestionIds = await (
                from w in db.WrongBookEntries.AsNoTracking()
                where w.UserId == userId
                join q in db.Questions.AsNoTracking() on w.QuestionId equals q.Id
                where request.DomainScope == null || q.Domain == request.DomainScope.Value
                select w.QuestionId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var wrongSet = wrongQuestionIds.ToHashSet();

        var contextQuestionIds = request.SelectedWrongQuestionIds.Count > 0
            ? request.SelectedWrongQuestionIds.Where(wrongSet.Contains).Distinct().ToList()
            : wrongQuestionIds;

        List<Question> contextQuestions;
        if (contextQuestionIds.Count == 0)
        {
            contextQuestions = new List<Question>();
        }
        else
        {
            contextQuestions = await db.Questions.AsNoTracking()
                .Where(q => contextQuestionIds.Contains(q.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var focusTags = InferFocusTags(contextQuestions);
        var focusKeywords = InferFocusKeywords(contextQuestions);

        var candidates = await (
                from q in db.Questions.AsNoTracking()
                where q.IsEnabled && !wrongSet.Contains(q.Id)
                where request.DomainScope == null || q.Domain == request.DomainScope.Value
                orderby q.Id descending
                select q)
            .Take(400)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var strict = candidates
            .Where(q => RecommendationMatcher.MatchesStrict(q, focusTags, focusKeywords))
            .OrderByDescending(q => ScoreLocal(q, focusTags, focusKeywords))
            .ThenByDescending(q => q.Id)
            .ToList();

        var final = strict.Select(q => q.Id).Take(TargetCount).ToList();
        if (final.Count < TargetCount)
        {
            foreach (var q in candidates.Where(q => RecommendationMatcher.MatchesRelaxed(q, focusTags, focusKeywords)))
            {
                if (final.Count >= TargetCount)
                {
                    break;
                }

                if (!final.Contains(q.Id))
                {
                    final.Add(q.Id);
                }
            }
        }

        if (final.Count < TargetCount)
        {
            foreach (var q in candidates)
            {
                if (final.Count >= TargetCount)
                {
                    break;
                }

                if (!final.Contains(q.Id))
                {
                    final.Add(q.Id);
                }
            }
        }

        _logger.LogInformation("AI 题目推荐（占位）：UserId={UserId}, Picked={Count}", userId, final.Count);

        return new QuestionRecommendationDto
        {
            Rationale = focusTags.Count == 0 && focusKeywords.Count == 0
                ? "暂无错题标签/关键词信号：在领域与排除错题约束下推荐最近题目。"
                : "基于错题 TopicTags/TopicKeywords 与知识点，在排除错题本后的候选集中筛选。",
            FocusTags = focusTags,
            FocusKeywords = focusKeywords,
            RecommendedQuestionIds = final.Take(TargetCount).ToList()
        };
    }

    private static IReadOnlyList<string> InferFocusTags(IReadOnlyList<Question> contextQuestions)
    {
        var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in contextQuestions)
        {
            foreach (var t in RecommendationMatcher.Tokenize(q.TopicTags))
            {
                bag[t] = bag.TryGetValue(t, out var c) ? c + 1 : 1;
            }

            foreach (var t in RecommendationMatcher.Tokenize(q.KnowledgeTags))
            {
                bag[t] = bag.TryGetValue(t, out var c) ? c + 1 : 1;
            }
        }

        return bag.OrderByDescending(kv => kv.Value).Take(6).Select(kv => kv.Key).ToList();
    }

    private static IReadOnlyList<string> InferFocusKeywords(IReadOnlyList<Question> contextQuestions)
    {
        var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in contextQuestions)
        {
            foreach (var t in RecommendationMatcher.Tokenize(q.TopicKeywords))
            {
                if (t.Length >= 2)
                {
                    bag[t] = bag.TryGetValue(t, out var c) ? c + 1 : 1;
                }
            }
        }

        return bag.OrderByDescending(kv => kv.Value).Take(6).Select(kv => kv.Key).ToList();
    }

    private static int ScoreLocal(Question q, IReadOnlyList<string> focusTags, IReadOnlyList<string> focusKeywords)
    {
        var s = 0;
        foreach (var t in focusTags)
        {
            if (RecommendationMatcher.TagFieldsContain(q, t))
            {
                s += 3;
            }
        }

        foreach (var k in focusKeywords)
        {
            if (RecommendationMatcher.KeywordHit(q, k))
            {
                s += 2;
            }
        }

        return s;
    }
}
