using AiSmartDrill.App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Infrastructure;

/// <summary>
/// 负责数据库创建与演示种子数据初始化：仅在首次创建库文件时写入种子，不在每次启动时删除已有 SQLite，以免用户与 AI 新增题目丢失。
/// </summary>
public sealed class DatabaseInitializer
{
    /// <summary>
    /// 演示用户的固定主键，用于简化 UI 与种子数据关联。
    /// </summary>
    public const long DemoUserId = 1L;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>
    /// 初始化 <see cref="DatabaseInitializer"/> 的新实例。
    /// </summary>
    /// <param name="dbFactory">数据库上下文工厂。</param>
    /// <param name="logger">日志记录器。</param>
    public DatabaseInitializer(IDbContextFactory<AppDbContext> dbFactory, ILogger<DatabaseInitializer> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// 异步初始化数据库结构与种子数据。
    /// </summary>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // 演示环境：使用 EnsureCreated 避免引入迁移复杂度；生产环境应改用 Migrate。
            // 若库文件已存在，返回 false，不得再次灌入种子，否则会与已有主键/数据冲突且会掩盖用户题库。
            var created = await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

            if (!created)
            {
                _logger.LogInformation("数据库已存在，跳过演示种子初始化（保留题库、答题与错题等数据）。");
                return;
            }

            _logger.LogInformation("新建数据库，写入演示种子数据...");

            var demoUser = new AppUser
            {
                Id = DemoUserId,
                DisplayName = "演示学员",
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Users.Add(demoUser);

            var questions = QuestionSeedBuilder.BuildAllSeedQuestions(DateTime.UtcNow);
            db.Questions.AddRange(questions);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // 若存在历史数据或手工录入导致标准答案为空，按题型补默认占位，避免判分与展示异常。
            var missingAnswer = await db.Questions
                .Where(q => q.StandardAnswer == null || q.StandardAnswer.Trim() == string.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var q in missingAnswer)
            {
                q.StandardAnswer = q.Type switch
                {
                    QuestionType.MultipleChoice => "A,C",
                    QuestionType.TrueFalse => "对",
                    QuestionType.ShortAnswer => "参考答案",
                    QuestionType.FillInBlank => "(关键|核心|重点)",
                    _ => "A"
                };
            }

            if (missingAnswer.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("已为 {Count} 道缺少标准答案的题目写入占位答案。", missingAnswer.Count);
            }

            // 预置若干历史答题与错题，便于统计与筛选演示。
            var sessionA = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var q1 = questions[0];
            var q2 = questions[1];
            var q3 = questions[2];

            db.AnswerRecords.AddRange(
                new AnswerRecord
                {
                    UserId = DemoUserId,
                    QuestionId = q1.Id,
                    SessionId = sessionA,
                    UserAnswer = "A",
                    IsCorrect = true,
                    Score = 1m,
                    DurationMs = 1200,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                },
                new AnswerRecord
                {
                    UserId = DemoUserId,
                    QuestionId = q2.Id,
                    SessionId = sessionA,
                    UserAnswer = "错",
                    IsCorrect = false,
                    Score = 0m,
                    DurationMs = 5400,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                },
                new AnswerRecord
                {
                    UserId = DemoUserId,
                    QuestionId = q3.Id,
                    SessionId = sessionA,
                    UserAnswer = "B",
                    IsCorrect = false,
                    Score = 0m,
                    DurationMs = 8000,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                });

            db.WrongBookEntries.AddRange(
                new WrongBookEntry
                {
                    UserId = DemoUserId,
                    QuestionId = q2.Id,
                    WrongCount = 2,
                    LastWrongAtUtc = DateTime.UtcNow.AddDays(-1)
                },
                new WrongBookEntry
                {
                    UserId = DemoUserId,
                    QuestionId = q3.Id,
                    WrongCount = 1,
                    LastWrongAtUtc = DateTime.UtcNow.AddDays(-2)
                });

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("演示种子数据写入完成。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }

}
