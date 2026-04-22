using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 在需要「落领域」时，结合规则推断与题库已有领域分布，避免重复造轮子。
/// </summary>
public static class CareerPathDomainResolution
{
    /// <summary>
    /// 先按关键词推断领域；若该领域在题库中已有题目则采用；
    /// 否则在「题库中已有题目的领域」里选与技能/JD 文本得分最高者；
    /// 若仍无，返回规则推断结果（可能为 <see cref="QuestionDomain.Uncategorized"/>）。
    /// </summary>
    public static async Task<QuestionDomain> ResolveDomainForGenerationAsync(
        IDbContextFactory<AppDbContext> dbFactory,
        IReadOnlyList<string> skills,
        string? jobSummary,
        CancellationToken cancellationToken = default)
    {
        var inferred = CareerPathDomainInference.InferDomain(
            CareerPathDomainInference.NormalizeSkillHints(skills, 12),
            jobSummary);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var domainsWithData = await db.Questions.AsNoTracking()
            .Where(x => x.IsEnabled)
            .GroupBy(x => x.Domain)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (domainsWithData.Contains(inferred))
        {
            return inferred;
        }

        var hay = BuildHaystackStrings(skills, jobSummary);
        QuestionDomain? best = null;
        var bestScore = 0;
        foreach (var d in domainsWithData)
        {
            var score = ScoreDomainLabelAgainstHaystack(d, hay);
            if (score > bestScore)
            {
                bestScore = score;
                best = d;
            }
        }

        if (bestScore > 0 && best is { } pick)
        {
            return pick;
        }

        return inferred;
    }

    private static IReadOnlyList<string> BuildHaystackStrings(IReadOnlyList<string> skills, string? jobSummary)
    {
        var list = new List<string>();
        foreach (var s in skills)
        {
            var t = (s ?? string.Empty).Trim();
            if (t.Length > 0)
            {
                list.Add(t);
            }
        }

        if (!string.IsNullOrWhiteSpace(jobSummary))
        {
            list.Add(jobSummary.Trim());
        }

        return list;
    }

    private static int ScoreDomainLabelAgainstHaystack(QuestionDomain domain, IReadOnlyList<string> haystacks)
    {
        var label = CareerPathDomainInference.MapDomainDisplay(domain);
        var aliases = new[] { label, domain.ToString() };
        var score = 0;
        foreach (var hay in haystacks)
        {
            if (hay.Length < 2)
            {
                continue;
            }

            var h = hay;
            foreach (var a in aliases)
            {
                if (a.Length < 2)
                {
                    continue;
                }

                if (h.Contains(a, StringComparison.OrdinalIgnoreCase) ||
                    a.Contains(h, StringComparison.OrdinalIgnoreCase))
                {
                    score += 3;
                }
            }
        }

        return score;
    }
}
