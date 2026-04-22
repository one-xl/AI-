using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 从已入库题目聚合「细知识点」短语表，供 AI 推荐与出题时复用已有标签；并提供与题库的近似匹配归一化。
/// </summary>
public static class KnowledgePointCatalogQuery
{
    /// <summary>
    /// 默认注入提示词或辅助筛选时的知识点条数上限。
    /// </summary>
    public const int DefaultPromptCatalogLimit = 72;

    private static readonly HashSet<string> NoiseTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "AI生成",
        "未分类"
    };

    /// <summary>
    /// 从启用题目中读取 <see cref="Question.PrimaryKnowledgePoint"/> 与 <see cref="Question.KnowledgeTags"/> 分词，
    /// 按出现频次降序返回去重短语列表（主知识点加权更高）。
    /// </summary>
    /// <param name="db">数据库上下文。</param>
    /// <param name="domain">为 null 时统计全库；否则限定该 <see cref="Question.Domain"/>。</param>
    /// <param name="maxItems">返回的最大条数。</param>
    /// <param name="cancellationToken">取消标记。</param>
    public static async Task<IReadOnlyList<string>> LoadOrderedByFrequencyAsync(
        AppDbContext db,
        QuestionDomain? domain,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        if (maxItems <= 0)
        {
            return Array.Empty<string>();
        }

        var q = db.Questions.AsNoTracking().Where(x => x.IsEnabled);
        if (domain is { } dom)
        {
            q = q.Where(x => x.Domain == dom);
        }

        var rows = await q
            .Select(x => new { x.PrimaryKnowledgePoint, x.KnowledgeTags })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var pk = (row.PrimaryKnowledgePoint ?? string.Empty).Trim();
            if (pk.Length > 0 && !IsNoise(pk))
            {
                bag[pk] = bag.TryGetValue(pk, out var c) ? c + 2 : 2;
            }

            foreach (var t in RecommendationMatcher.Tokenize(row.KnowledgeTags))
            {
                if (IsNoise(t))
                {
                    continue;
                }

                bag[t] = bag.TryGetValue(t, out var c2) ? c2 + 1 : 1;
            }
        }

        return bag
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(maxItems)
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>
    /// 在未显式指定 <see cref="QuestionRecommendationRequest.DomainScope"/> 且无错题上下文时，
    /// 按错题本中题目出现次数最多的 <see cref="Question.Domain"/> 推断推荐与知识点目录所属领域，避免混用全库短语。
    /// </summary>
    /// <param name="db">数据库上下文。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="request">推荐请求；若 <see cref="QuestionRecommendationRequest.DomainScope"/> 已限定，则仅统计该领域下的错题。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>错题本非空时返回众数领域；否则 null。</returns>
    public static async Task<QuestionDomain?> InferDominantDomainFromWrongBookAsync(
        AppDbContext db,
        long userId,
        QuestionRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var domains = await (
                from w in db.WrongBookEntries.AsNoTracking()
                where w.UserId == userId
                join q in db.Questions.AsNoTracking() on w.QuestionId equals q.Id
                where request.DomainScope == null || q.Domain == request.DomainScope.Value
                select q.Domain)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (domains.Count == 0)
        {
            return null;
        }

        return domains
            .GroupBy(d => d)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .First();
    }

    /// <summary>
    /// 在已按领域与频次排序的短语表中，筛出与「主知识点锚点」及弱项提示相关的子集，用于推荐提示词，降低无关短语干扰。
    /// </summary>
    /// <param name="orderedCatalog">按频次降序的短语列表（通常来自 <see cref="LoadOrderedByFrequencyAsync"/>）。</param>
    /// <param name="anchorPrimaryKnowledgePoint">错题推断的主知识点或模型前置锚点。</param>
    /// <param name="relatedWeakKnowledgePoints">弱项细知识点等补充锚点（可为 null）。</param>
    /// <param name="maxItems">返回条数上限。</param>
    public static IReadOnlyList<string> FilterCatalogForPrompt(
        IReadOnlyList<string> orderedCatalog,
        string? anchorPrimaryKnowledgePoint,
        IReadOnlyList<string>? relatedWeakKnowledgePoints,
        int maxItems)
    {
        if (maxItems <= 0 || orderedCatalog.Count == 0)
        {
            return Array.Empty<string>();
        }

        var cap = Math.Min(maxItems, orderedCatalog.Count);
        var anchors = new List<string>();
        void TryAddAnchor(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return;
            }

            var t = s.Trim();
            if (IsNoise(t))
            {
                return;
            }

            foreach (var a in anchors)
            {
                if (a.Equals(t, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            anchors.Add(t);
            if (anchors.Count >= 12)
            {
                return;
            }
        }

        TryAddAnchor(anchorPrimaryKnowledgePoint);
        if (relatedWeakKnowledgePoints is not null)
        {
            foreach (var w in relatedWeakKnowledgePoints)
            {
                TryAddAnchor(w);
                if (anchors.Count >= 12)
                {
                    break;
                }
            }
        }

        if (anchors.Count == 0)
        {
            return orderedCatalog.Take(cap).ToList();
        }

        static bool RelatesLoose(string item, string anchor)
        {
            if (item.Length < 1 || anchor.Length < 1)
            {
                return false;
            }

            if (item.Equals(anchor, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (item.Length >= 2 && anchor.Length >= 2)
            {
                return item.Contains(anchor, StringComparison.OrdinalIgnoreCase) ||
                       anchor.Contains(item, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        var picked = new List<string>();
        var pickedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in orderedCatalog)
        {
            if (picked.Count >= cap)
            {
                break;
            }

            if (anchors.Any(an => RelatesLoose(item, an)) && pickedSet.Add(item))
            {
                picked.Add(item);
            }
        }

        var minFill = Math.Min(8, Math.Max(1, cap / 2));
        if (picked.Count < minFill)
        {
            foreach (var item in orderedCatalog)
            {
                if (picked.Count >= cap)
                {
                    break;
                }

                if (pickedSet.Add(item))
                {
                    picked.Add(item);
                }
            }
        }

        return picked;
    }

    /// <summary>
    /// 将模型或错题推断出的短语归一为题库中已有条目的规范写法：先精确匹配，再最长包含匹配。
    /// </summary>
    /// <returns>与 <paramref name="orderedCatalog"/> 中某一项对齐后的字符串；无法对齐时返回 null（表示保留原短语作为新知识点）。</returns>
    public static string? ResolveCanonicalTag(string? candidate, IReadOnlyList<string> orderedCatalog)
    {
        if (string.IsNullOrWhiteSpace(candidate) || orderedCatalog.Count == 0)
        {
            return null;
        }

        var raw = candidate.Trim();
        if (IsNoise(raw))
        {
            return null;
        }

        foreach (var known in orderedCatalog)
        {
            if (known.Equals(raw, StringComparison.OrdinalIgnoreCase))
            {
                return known;
            }
        }

        string? best = null;
        var bestLen = 0;
        foreach (var known in orderedCatalog)
        {
            if (known.Length < 2 || raw.Length < 1)
            {
                continue;
            }

            if (raw.Contains(known, StringComparison.OrdinalIgnoreCase) ||
                known.Contains(raw, StringComparison.OrdinalIgnoreCase))
            {
                if (known.Length > bestLen)
                {
                    bestLen = known.Length;
                    best = known;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// 用题库短语表规范化题目的 <see cref="Question.KnowledgeTags"/> 与 <see cref="Question.PrimaryKnowledgePoint"/>：
    /// 各分词尽量映射为已有规范短语；无法映射的保留为「新知识点」。
    /// </summary>
    public static void AlignQuestionKnowledgeFields(Question q, IReadOnlyList<string> catalog)
    {
        if (catalog.Count == 0)
        {
            return;
        }

        const int maxTagsLen = 512;
        const int maxPrimaryLen = 128;

        var mapped = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in RecommendationMatcher.Tokenize(q.KnowledgeTags))
        {
            var use = ResolveCanonicalTag(t, catalog) ?? t.Trim();
            if (use.Length == 0 || IsNoise(use))
            {
                continue;
            }

            if (seen.Add(use))
            {
                mapped.Add(use);
            }
        }

        var primaryRaw = (q.PrimaryKnowledgePoint ?? string.Empty).Trim();
        if (primaryRaw.Length > 0 && !IsNoise(primaryRaw))
        {
            var pUse = ResolveCanonicalTag(primaryRaw, catalog) ?? primaryRaw;
            if (seen.Add(pUse))
            {
                mapped.Insert(0, pUse);
            }
        }

        if (mapped.Count == 0)
        {
            return;
        }

        var tags = string.Join(",", mapped);
        if (tags.Length > maxTagsLen)
        {
            tags = tags[..maxTagsLen];
        }

        q.KnowledgeTags = tags;

        var primary = ResolveCanonicalTag(primaryRaw, catalog)
                      ?? ResolveCanonicalTag(mapped[0], catalog)
                      ?? mapped[0];
        if (primary.Length > maxPrimaryLen)
        {
            primary = primary[..maxPrimaryLen];
        }

        q.PrimaryKnowledgePoint = primary;
    }

    /// <summary>
    /// 将推断出的主知识点短语对齐到题库规范短语（用于推荐筛选与模型提示）。
    /// </summary>
    public static string? AlignFocusKnowledgePoint(string? inferred, IReadOnlyList<string> catalog)
    {
        if (string.IsNullOrWhiteSpace(inferred))
        {
            return null;
        }

        var t = inferred.Trim();
        if (catalog.Count == 0)
        {
            return t;
        }

        return ResolveCanonicalTag(t, catalog) ?? t;
    }

    private static bool IsNoise(string t)
    {
        var s = t.Trim();
        if (s.Length == 0)
        {
            return true;
        }

        if (NoiseTags.Contains(s))
        {
            return true;
        }

        return KnowledgeTagStopwords.IsStopword(s);
    }
}
