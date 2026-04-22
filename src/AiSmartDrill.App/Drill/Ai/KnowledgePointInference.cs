using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 从错题上下文推断主知识点与多数领域，供推荐筛选与学习计划摘要使用。
/// </summary>
internal static class KnowledgePointInference
{
    /// <summary>
    /// 从错题题目列表推断出现频率最高的细知识点（<see cref="Question.PrimaryKnowledgePoint"/> 加权更高）。
    /// </summary>
    public static string? InferPrimaryKnowledgePoint(IReadOnlyList<Question> contextQuestions)
    {
        if (contextQuestions.Count == 0)
        {
            return null;
        }

        var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in contextQuestions)
        {
            var pk = (q.PrimaryKnowledgePoint ?? string.Empty).Trim();
            if (pk.Length > 0 && !KnowledgeTagStopwords.IsStopword(pk))
            {
                bag[pk] = bag.TryGetValue(pk, out var c) ? c + 2 : 2;
            }

            foreach (var t in RecommendationMatcher.Tokenize(q.KnowledgeTags))
            {
                if (KnowledgeTagStopwords.IsStopword(t) || t.Length < 2)
                {
                    continue;
                }

                bag[t] = bag.TryGetValue(t, out var c2) ? c2 + 1 : 1;
            }
        }

        if (bag.Count == 0)
        {
            return null;
        }

        return bag
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => kv.Key)
            .First();
    }

    /// <summary>
    /// 取错题上下文中占多数的 <see cref="Question.Domain"/>；无条目时返回 null。
    /// </summary>
    public static QuestionDomain? InferMajorityDomain(IReadOnlyList<Question> contextQuestions)
    {
        if (contextQuestions.Count == 0)
        {
            return null;
        }

        return contextQuestions
            .GroupBy(q => q.Domain)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .First();
    }
}
