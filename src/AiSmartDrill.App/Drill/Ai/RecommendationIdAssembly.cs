using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 将 Ark 或本地推断的标签、关键词与主知识点约束合并为最终推荐题目 Id 列表。
/// </summary>
internal static class RecommendationIdAssembly
{
    /// <summary>
    /// 按「同主知识点优先 → 标签/关键词严格 → 宽松 → 必要时放弃知识点约束」组装推荐 Id。
    /// </summary>
    public static (List<long> Ids, bool RelaxedKnowledgePoint) AssembleRecommendedIds(
        IReadOnlyList<Question> candidates,
        IReadOnlyList<string> focusTags,
        IReadOnlyList<string> focusKeywords,
        string? resolvedKnowledgePoint,
        IReadOnlyList<long> aiPickedOrder,
        int targetCount)
    {
        var kpRequired = !string.IsNullOrWhiteSpace(resolvedKnowledgePoint);
        var relaxedKp = false;
        var final = new List<long>();

        void TryAdd(Question? q)
        {
            if (q is null || final.Count >= targetCount || final.Contains(q.Id))
            {
                return;
            }

            final.Add(q.Id);
        }

        bool KpOk(Question q) => !kpRequired || RecommendationMatcher.KnowledgePointHit(q, resolvedKnowledgePoint);

        foreach (var id in aiPickedOrder)
        {
            var q = candidates.FirstOrDefault(x => x.Id == id);
            if (q is null || !KpOk(q) || !RecommendationMatcher.MatchesStrict(q, focusTags, focusKeywords))
            {
                continue;
            }

            TryAdd(q);
        }

        var tierStrictKp = candidates
            .Where(q => KpOk(q) && RecommendationMatcher.MatchesStrict(q, focusTags, focusKeywords))
            .OrderByDescending(q => ScoreQuestionWithKnowledgePoint(q, focusTags, focusKeywords, resolvedKnowledgePoint))
            .ThenByDescending(q => q.Id)
            .ToList();
        foreach (var q in tierStrictKp)
        {
            TryAdd(q);
        }

        if (final.Count < targetCount)
        {
            foreach (var q in candidates
                         .Where(q => KpOk(q) && RecommendationMatcher.MatchesRelaxed(q, focusTags, focusKeywords))
                         .OrderByDescending(x =>
                             ScoreQuestionWithKnowledgePoint(x, focusTags, focusKeywords, resolvedKnowledgePoint))
                         .ThenByDescending(q => q.Id))
            {
                TryAdd(q);
            }
        }

        if (final.Count < targetCount && kpRequired)
        {
            relaxedKp = true;
            foreach (var q in candidates
                         .Where(q => RecommendationMatcher.MatchesStrict(q, focusTags, focusKeywords))
                         .OrderByDescending(q => ScoreQuestion(q, focusTags, focusKeywords))
                         .ThenByDescending(q => q.Id))
            {
                TryAdd(q);
            }
        }

        if (final.Count < targetCount && kpRequired)
        {
            foreach (var q in candidates
                         .Where(q => RecommendationMatcher.MatchesRelaxed(q, focusTags, focusKeywords))
                         .OrderByDescending(q => ScoreQuestion(q, focusTags, focusKeywords))
                         .ThenByDescending(q => q.Id))
            {
                TryAdd(q);
            }
        }

        if (final.Count < targetCount)
        {
            foreach (var q in candidates.OrderByDescending(x => x.Id))
            {
                TryAdd(q);
            }
        }

        return (final, relaxedKp);
    }

    private static int ScoreQuestion(Question q, IReadOnlyList<string> focusTags, IReadOnlyList<string> focusKeywords)
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

    private static int ScoreQuestionWithKnowledgePoint(
        Question q,
        IReadOnlyList<string> focusTags,
        IReadOnlyList<string> focusKeywords,
        string? resolvedKnowledgePoint)
    {
        var s = ScoreQuestion(q, focusTags, focusKeywords);
        if (!string.IsNullOrWhiteSpace(resolvedKnowledgePoint) &&
            RecommendationMatcher.KnowledgePointHit(q, resolvedKnowledgePoint))
        {
            s += 24;
        }

        return s;
    }
}
