using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 从答题与错题本聚合 <see cref="UserPerformanceSummary"/>，区分模块弱项与细知识点弱项。
/// </summary>
internal static class UserPerformanceSummaryFactory
{
    /// <summary>
    /// 异步构建指定用户的整体表现摘要。
    /// </summary>
    public static async Task<UserPerformanceSummary> CreateAsync(
        AppDbContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var wrongEntries = await db.WrongBookEntries.AsNoTracking()
            .Where(w => w.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var answerRecords = await db.AnswerRecords.AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var weakTopics = await TopWeakTopicTagsAsync(db, userId, cancellationToken).ConfigureAwait(false);
        var weakKp = await TopWeakKnowledgePointsAsync(db, userId, cancellationToken).ConfigureAwait(false);

        return new UserPerformanceSummary
        {
            UserId = userId,
            TotalAttempts = answerRecords.Count,
            CorrectAttempts = answerRecords.Count(a => a.IsCorrect),
            WrongBookCount = wrongEntries.Count,
            WeakTags = weakKp.Select(x => x.Name).ToList(),
            WeakKnowledgePoints = weakKp.Select(x => x.Name).ToList(),
            WeakTopicTags = weakTopics.Select(x => x.Name).ToList(),
            WeakKnowledgePointStats = weakKp,
            WeakTopicTagStats = weakTopics
        };
    }

    private static async Task<IReadOnlyList<WeaknessStatDto>> TopWeakTopicTagsAsync(
        AppDbContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var topicRows = await db.WrongBookEntries
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (_, q) => q.TopicTags)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var tags in topicRows)
        {
            foreach (var t in RecommendationMatcher.Tokenize(tags))
            {
                if (t.Length < 1)
                {
                    continue;
                }

                bag[t] = bag.TryGetValue(t, out var c) ? c + 1 : 1;
            }
        }

        return bag
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => new WeaknessStatDto { Name = kv.Key, Count = kv.Value })
            .ToList();
    }

    private static async Task<IReadOnlyList<WeaknessStatDto>> TopWeakKnowledgePointsAsync(
        AppDbContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var questions = await db.WrongBookEntries
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Join(db.Questions.AsNoTracking(), w => w.QuestionId, q => q.Id, (_, q) => q)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in questions)
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

        return bag
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => new WeaknessStatDto { Name = kv.Key, Count = kv.Value })
            .ToList();
    }
}
