using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 题目与 FocusTags/FocusKeywords 的匹配工具（严格：标签维与关键词维同时满足各自约束；宽松：任一侧命中）。
/// </summary>
internal static class RecommendationMatcher
{
    private static readonly char[] Delims = { ',', '，', ';', '；', '|', '、' };

    /// <summary>
    /// 将模型或配置中的字符串列表规范化（去空、去重）。
    /// </summary>
    public static IReadOnlyList<string> NormalizeTokens(IEnumerable<string>? items)
    {
        if (items is null)
        {
            return Array.Empty<string>();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var x in items)
        {
            var t = (x ?? string.Empty).Trim();
            if (t.Length > 0)
            {
                set.Add(t);
            }
        }

        return set.ToList();
    }

    /// <summary>
    /// 拆分题目侧多值字段。
    /// </summary>
    public static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        foreach (var p in text.Split(Delims, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (p.Length > 0)
            {
                yield return p;
            }
        }
    }

    /// <summary>
    /// 严格匹配：无标签要求或任一标签命中；无关键词要求或任一关键词命中；两者同时成立。
    /// </summary>
    public static bool MatchesStrict(Question q, IReadOnlyList<string> focusTags, IReadOnlyList<string> focusKeywords)
    {
        var tagOk = focusTags.Count == 0 || focusTags.Any(t => TagFieldsContain(q, t));
        var kwOk = focusKeywords.Count == 0 || focusKeywords.Any(k => KeywordHit(q, k));
        return tagOk && kwOk;
    }

    /// <summary>
    /// 宽松匹配：至少一侧有要求时，标签或关键词任一命中即可。
    /// </summary>
    public static bool MatchesRelaxed(Question q, IReadOnlyList<string> focusTags, IReadOnlyList<string> focusKeywords)
    {
        if (focusTags.Count == 0 && focusKeywords.Count == 0)
        {
            return true;
        }

        var tagHit = focusTags.Any(t => TagFieldsContain(q, t));
        var kwHit = focusKeywords.Any(k => KeywordHit(q, k));
        return tagHit || kwHit;
    }

    /// <summary>
    /// 判断分类标签 token 是否出现在题目的 TopicTags 或 KnowledgeTags 分词中。
    /// </summary>
    public static bool TagFieldsContain(Question q, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        token = token.Trim();
        foreach (var bucket in new[] { q.TopicTags, q.KnowledgeTags })
        {
            foreach (var t in Tokenize(bucket))
            {
                if (t.Equals(token, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (t.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                    token.Contains(t, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 判断关键词是否出现在题干、TopicKeywords 或 KnowledgeTags 中。
    /// </summary>
    public static bool KeywordHit(Question q, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return false;
        }

        keyword = keyword.Trim();
        if (q.Stem.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var hay in new[] { q.TopicKeywords, q.KnowledgeTags, q.TopicTags })
        {
            if (!string.IsNullOrEmpty(hay) && hay.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断题目是否在「主知识点」上命中：<see cref="Question.PrimaryKnowledgePoint"/> 与
    /// <see cref="Question.KnowledgeTags"/> 分词与给定短语做等价或包含关系比对。
    /// </summary>
    /// <param name="q">候选题目。</param>
    /// <param name="point">主知识点短语；空表示不做知识点维约束。</param>
    public static bool KnowledgePointHit(Question q, string? point)
    {
        if (string.IsNullOrWhiteSpace(point))
        {
            return true;
        }

        point = point.Trim();
        var primary = (q.PrimaryKnowledgePoint ?? string.Empty).Trim();
        if (primary.Length > 0)
        {
            if (primary.Equals(point, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (primary.Contains(point, StringComparison.OrdinalIgnoreCase) ||
                point.Contains(primary, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var t in Tokenize(q.KnowledgeTags))
        {
            if (t.Equals(point, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (t.Length >= 2 && point.Length >= 2 &&
                (t.Contains(point, StringComparison.OrdinalIgnoreCase) ||
                 point.Contains(t, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
