using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 在 CareerPath 流程中统计题库内与技能短语匹配的题量，供准备对话框展示。
/// </summary>
public static class CareerPathQuestionInventory
{
    /// <summary>
    /// 每个技能短语在启用题目中的命中数量（题干/标签/主知识点等，规则同 <see cref="CareerPathQuestionFilter"/>）。
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, int>> GetPerSkillCountsAsync(
        IDbContextFactory<AppDbContext> dbFactory,
        IReadOnlyList<string> skills,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var qs = await db.Questions.AsNoTracking()
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in skills)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                continue;
            }

            var t = s.Trim();
            dict[t] = qs.Count(q => CareerPathQuestionFilter.MatchesAnySkill(q, new[] { t }));
        }

        return dict;
    }

    /// <summary>
    /// 命中至少一项技能的题目按 <see cref="Question.Domain"/> 分组计数。
    /// </summary>
    public static async Task<IReadOnlyDictionary<QuestionDomain, int>> GetMatchingCountsByDomainAsync(
        IDbContextFactory<AppDbContext> dbFactory,
        IReadOnlyList<string> skills,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var qs = await db.Questions.AsNoTracking()
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var matched = qs.Where(q => CareerPathQuestionFilter.MatchesAnySkill(q, skills)).ToList();
        return matched
            .GroupBy(x => x.Domain)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// 在指定领域与难度下，命中技能的题目总数（用于筛选后题量提示）。
    /// </summary>
    public static async Task<int> CountMatchingAsync(
        IDbContextFactory<AppDbContext> dbFactory,
        IReadOnlyList<string> skills,
        QuestionDomain? domainScope,
        DifficultyLevel? difficultyScope,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var q = db.Questions.AsNoTracking().Where(x => x.IsEnabled);
        if (domainScope is { } dom)
        {
            q = q.Where(x => x.Domain == dom);
        }

        if (difficultyScope is { } df)
        {
            q = q.Where(x => x.Difficulty == df);
        }

        var list = await q.ToListAsync(cancellationToken).ConfigureAwait(false);
        return list.Count(x => CareerPathQuestionFilter.MatchesAnySkill(x, skills));
    }
}
