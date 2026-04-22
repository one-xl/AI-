using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 本地占位实现的题目推荐：与云端版一致的排除错题、领域范围、主知识点与标签/关键词筛选逻辑。
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

        var performanceSummary = await UserPerformanceSummaryFactory.CreateAsync(db, userId, cancellationToken)
            .ConfigureAwait(false);

        var effectiveDomain = request.DomainScope
                              ?? KnowledgePointInference.InferMajorityDomain(contextQuestions)
                              ?? await KnowledgePointCatalogQuery.InferDominantDomainFromWrongBookAsync(
                                      db,
                                      userId,
                                      request,
                                      cancellationToken)
                                  .ConfigureAwait(false);
        var candidates = await (
                from q in db.Questions.AsNoTracking()
                where q.IsEnabled && !wrongSet.Contains(q.Id)
                where effectiveDomain == null || q.Domain == effectiveDomain.Value
                orderby q.Id descending
                select q)
            .Take(400)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var focusTags = MergeTokenLists(InferFocusTags(contextQuestions), request.ExternalSkillHints);
        var focusKeywords = MergeTokenLists(InferFocusKeywords(contextQuestions), request.ExternalSkillHints);
        const int catalogAlignPool = 200;
        IReadOnlyList<string> fullKpCatalog = effectiveDomain is { } catalogDom
            ? await KnowledgePointCatalogQuery.LoadOrderedByFrequencyAsync(
                    db,
                    catalogDom,
                    catalogAlignPool,
                    cancellationToken)
                .ConfigureAwait(false)
            : Array.Empty<string>();
        var resolvedKpRaw = KnowledgePointInference.InferPrimaryKnowledgePoint(contextQuestions)
                            ?? (request.ExternalSkillHints.Count > 0 ? request.ExternalSkillHints[0] : null)
                            ?? (performanceSummary.WeakKnowledgePoints.Count > 0
                                ? performanceSummary.WeakKnowledgePoints[0]
                                : null);
        var resolvedKp = KnowledgePointCatalogQuery.AlignFocusKnowledgePoint(resolvedKpRaw, fullKpCatalog);

        var (final, relaxedKp) = RecommendationIdAssembly.AssembleRecommendedIds(
            candidates,
            focusTags,
            focusKeywords,
            resolvedKp,
            Array.Empty<long>(),
            TargetCount);

        if (final.Count < TargetCount)
        {
            var moreIds = await db.Questions.AsNoTracking()
                .Where(q => q.IsEnabled && !wrongSet.Contains(q.Id))
                .Where(q => effectiveDomain == null || q.Domain == effectiveDomain.Value)
                .OrderByDescending(q => q.Id)
                .Select(q => q.Id)
                .Take(TargetCount * 2)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var id in moreIds)
            {
                if (final.Count >= TargetCount)
                {
                    break;
                }

                if (!final.Contains(id))
                {
                    final.Add(id);
                }
            }
        }

        var rationale = focusTags.Count == 0 && focusKeywords.Count == 0
            ? "暂无错题标签/关键词信号：在领域与排除错题约束下推荐最近题目。"
            : "基于错题主知识点与 TopicTags/TopicKeywords，在排除错题本后的候选集中筛选。";
        if (relaxedKp)
        {
            rationale += "（同知识点候选不足，已部分放宽）";
        }

        _logger.LogInformation("AI 题目推荐（占位）：UserId={UserId}, Picked={Count}", userId, final.Count);

        return new QuestionRecommendationDto
        {
            Rationale = rationale,
            FocusKnowledgePoint = resolvedKp,
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
                if (!KnowledgeTagStopwords.IsStopword(t))
                {
                    bag[t] = bag.TryGetValue(t, out var c) ? c + 1 : 1;
                }
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

    private static IReadOnlyList<string> MergeTokenLists(IReadOnlyList<string> baseTokens, IReadOnlyList<string> extra)
    {
        if (extra.Count == 0)
        {
            return baseTokens;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        foreach (var x in baseTokens)
        {
            var t = (x ?? string.Empty).Trim();
            if (t.Length > 0 && seen.Add(t))
            {
                list.Add(t);
            }
        }

        foreach (var x in extra)
        {
            var t = (x ?? string.Empty).Trim();
            if (t.Length > 0 && seen.Add(t))
            {
                list.Add(t);
            }
        }

        return list;
    }
}

