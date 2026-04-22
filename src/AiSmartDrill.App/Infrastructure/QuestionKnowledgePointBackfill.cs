using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Drill.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Infrastructure;

/// <summary>
/// 在本地库中补全缺失的 <see cref="Question.PrimaryKnowledgePoint"/> 与无效/空的
/// <see cref="Question.KnowledgeTags"/>：优先沿用已有细标签分词，其次用语干关键词路由，最后使用领域级兜底短语；
/// 不调用大模型，所有新写入短语均与领域或题干可核对关键词绑定。
/// </summary>
public static class QuestionKnowledgePointBackfill
{
    private const int MaxPrimaryLen = 128;
    private const int MaxKnowledgeTagsLen = 512;

    /// <summary>
    /// 扫描题库并写回可推导的知识点字段，返回实际更新题目条数。
    /// </summary>
    public static async Task<int> BackfillAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var list = await db.Questions.AsTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var changed = 0;

        foreach (var q in list)
        {
            var beforePrimary = q.PrimaryKnowledgePoint;
            var beforeTags = q.KnowledgeTags;
            if (!TryPatchQuestion(q, logger, out var reason))
            {
                continue;
            }

            if (!string.Equals(beforePrimary, q.PrimaryKnowledgePoint, StringComparison.Ordinal) ||
                !string.Equals(beforeTags, q.KnowledgeTags, StringComparison.Ordinal))
            {
                changed++;
                logger.LogDebug("知识点补全 Id={Id}: {Reason}", q.Id, reason);
            }
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (changed > 0)
        {
            logger.LogInformation("知识点补全：已更新 {Count} 道题目。", changed);
        }

        return changed;
    }

    private static bool TryPatchQuestion(Question q, ILogger logger, out string reason)
    {
        reason = string.Empty;
        var primaryTrim = (q.PrimaryKnowledgePoint ?? string.Empty).Trim();
        var hasPrimary = primaryTrim.Length > 0 && !string.Equals(primaryTrim, "未分类", StringComparison.OrdinalIgnoreCase);
        var tagsNeedRefresh = NeedsKnowledgeTagsRefresh(q.KnowledgeTags);

        if (hasPrimary && !tagsNeedRefresh)
        {
            return false;
        }

        var hay = BuildHaystack(q);

        if (hasPrimary && tagsNeedRefresh)
        {
            var p = ClipPrimary(primaryTrim);
            q.KnowledgeTags = ClipKnowledgeTags($"{p},{SeedKnowledgePointCatalog.PickSecondaryDistinct(q.Domain, p, q.Id)}");
            reason = "主知识点已存在，仅补全细标签列表";
            return true;
        }

        var fromTags = DerivePrimaryFromKnowledgeTags(q.KnowledgeTags);
        if (fromTags is not null)
        {
            q.PrimaryKnowledgePoint = ClipPrimary(fromTags);
            if (tagsNeedRefresh)
            {
                q.KnowledgeTags = ClipKnowledgeTags(
                    $"{q.PrimaryKnowledgePoint},{SeedKnowledgePointCatalog.PickSecondaryDistinct(q.Domain, q.PrimaryKnowledgePoint, q.Id)}");
            }

            reason = "由已有 KnowledgeTags 分词推导主知识点";
            return true;
        }

        var routed = StemToKnowledgePointRouter.TryMatch(q.Domain, hay);
        if (routed is not null)
        {
            q.PrimaryKnowledgePoint = ClipPrimary(routed);
            q.KnowledgeTags = ClipKnowledgeTags(
                $"{q.PrimaryKnowledgePoint},{SeedKnowledgePointCatalog.PickSecondaryDistinct(q.Domain, q.PrimaryKnowledgePoint, q.Id)}");
            reason = "题干/选项关键词路由";
            return true;
        }

        if (string.IsNullOrWhiteSpace(hay))
        {
            logger.LogWarning("知识点补全跳过 Id={Id}：题干与选项均为空，无法与题目关联。", q.Id);
            return false;
        }

        var fallback = SeedKnowledgePointCatalog.DomainCoarseFallbackPrimary(q.Domain);
        q.PrimaryKnowledgePoint = ClipPrimary(fallback);
        q.KnowledgeTags = ClipKnowledgeTags(
            $"{q.PrimaryKnowledgePoint},{SeedKnowledgePointCatalog.PickSecondaryDistinct(q.Domain, q.PrimaryKnowledgePoint, q.Id)}");
        reason = "领域兜底（无关键词命中）";
        return true;
    }

    private static string BuildHaystack(Question q)
    {
        var stem = q.Stem ?? string.Empty;
        var opt = q.OptionsJson ?? string.Empty;
        return $"{stem} {opt}";
    }

    private static bool NeedsKnowledgeTagsRefresh(string? knowledgeTags)
    {
        if (string.IsNullOrWhiteSpace(knowledgeTags))
        {
            return true;
        }

        var t = knowledgeTags.Trim();
        if (string.Equals(t, "未分类", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var tokens = RecommendationMatcher.Tokenize(t).ToList();
        if (tokens.Count == 0)
        {
            return true;
        }

        return tokens.All(KnowledgeTagStopwords.IsStopword);
    }

    private static string? DerivePrimaryFromKnowledgeTags(string? knowledgeTags)
    {
        foreach (var token in RecommendationMatcher.Tokenize(knowledgeTags))
        {
            if (!KnowledgeTagStopwords.IsStopword(token) && token.Trim().Length > 0)
            {
                return token.Trim();
            }
        }

        return null;
    }

    private static string ClipPrimary(string s) =>
        s.Length <= MaxPrimaryLen ? s : s[..MaxPrimaryLen];

    private static string ClipKnowledgeTags(string s) =>
        s.Length <= MaxKnowledgeTagsLen ? s : s[..MaxKnowledgeTagsLen];
}
