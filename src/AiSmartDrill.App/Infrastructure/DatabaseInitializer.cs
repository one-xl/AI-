using System.IO;
using AiSmartDrill.App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Infrastructure;

/// <summary>
/// 负责数据库创建与演示种子数据初始化，保证首次启动即可完整演示核心流程。
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

            // 尝试检查数据库是否需要重建（因为我们添加了新列）
            bool needsRebuild = false;
            try
            {
                // 尝试查询是否有 Domain 列
                _ = await db.Questions.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                // 如果查询失败，说明数据库结构不匹配，需要重建
                needsRebuild = true;
                _logger.LogInformation("检测到数据库结构不匹配，准备重建数据库...");
            }

            // 如果需要重建，先删除旧数据库文件
            if (needsRebuild)
            {
                var dbPath = db.Database.GetDbConnection().DataSource;
                if (File.Exists(dbPath))
                {
                    try
                    {
                        File.Delete(dbPath);
                        _logger.LogInformation("已删除旧数据库文件：{DbPath}", dbPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除旧数据库文件失败，尝试继续...");
                    }
                }
            }

            // 演示环境：使用 EnsureCreated 避免引入迁移复杂度；生产环境应改用 Migrate。
            await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Users.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("数据库已存在演示数据，跳过种子写入。");
                return;
            }

            _logger.LogInformation("写入演示种子数据...");

            var demoUser = new AppUser
            {
                Id = DemoUserId,
                DisplayName = "演示学员",
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Users.Add(demoUser);

            var questions = BuildSeedQuestions(DateTime.UtcNow);
            db.Questions.AddRange(questions);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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

    /// <summary>
    /// 构建题库演示数据，覆盖多题型、多难度与多知识点标签。
    /// </summary>
    /// <param name="nowUtc">种子时间戳（UTC）。</param>
    /// <returns>题目集合。</returns>
    private static List<Question> BuildSeedQuestions(DateTime nowUtc)
    {
        // 选项 JSON 采用简单数组文本，UI 侧可按需解析；判分以 StandardAnswer 为准。
        string Opt(params string[] lines) => System.Text.Json.JsonSerializer.Serialize(lines);

        return new List<Question>
        {
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Easy,
                Domain = QuestionDomain.CSharp,
                Stem = "C# 中值类型与引用类型的关键区别是什么？",
                StandardAnswer = "A",
                OptionsJson = Opt("A. 值类型通常分配在栈上，引用类型的变量保存对象引用", "B. 引用类型不能为 null", "C. 值类型一定比引用类型更快", "D. 二者没有区别"),
                KnowledgeTags = "C#,基础,类型系统",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.TrueFalse,
                Difficulty = DifficultyLevel.Easy,
                Domain = QuestionDomain.CSharp,
                Stem = "async/await 关键字会创建新的操作系统线程。",
                StandardAnswer = "错",
                OptionsJson = null,
                KnowledgeTags = "C#,异步",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Medium,
                Domain = QuestionDomain.CSharp,
                Stem = "EF Core 中 DbContext 的主要职责不包括哪一项？",
                StandardAnswer = "B",
                OptionsJson = Opt("A. 跟踪实体变更", "B. 替代 SQL Server 执行查询优化器", "C. 生成并执行 SQL", "D. 配置实体映射"),
                KnowledgeTags = "EFCore,数据访问",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.ShortAnswer,
                Difficulty = DifficultyLevel.Medium,
                Domain = QuestionDomain.DataStructure,
                Stem = "简述依赖注入（DI）在桌面应用中的两个好处。",
                StandardAnswer = "解耦;可测试",
                OptionsJson = null,
                KnowledgeTags = "架构,DI",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Hard,
                Domain = QuestionDomain.CSharp,
                Stem = "WPF 数据绑定中 INotifyPropertyChanged 的作用是？",
                StandardAnswer = "C",
                OptionsJson = Opt("A. 提升渲染性能", "B. 自动生成 XAML", "C. 在属性变更时通知 UI 更新", "D. 仅用于集合绑定"),
                KnowledgeTags = "WPF,MVVM",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.MultipleChoice,
                Difficulty = DifficultyLevel.Medium,
                Domain = QuestionDomain.Database,
                Stem = "以下哪些属于关系数据库范式化的常见目标？（多选）",
                StandardAnswer = "A,C",
                OptionsJson = Opt("A. 减少冗余", "B. 提升 CPU 主频", "C. 避免更新异常", "D. 让表名更短"),
                KnowledgeTags = "数据库,范式",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Easy,
                Domain = QuestionDomain.Database,
                Stem = "SQLite 作为嵌入式数据库的典型优势是？",
                StandardAnswer = "D",
                OptionsJson = Opt("A. 必须安装服务器", "B. 不支持事务", "C. 仅支持单表", "D. 文件型部署，便于分发"),
                KnowledgeTags = "SQLite,数据库",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.TrueFalse,
                Difficulty = DifficultyLevel.Medium,
                Domain = QuestionDomain.Database,
                Stem = "唯一索引可以保证 (UserId, QuestionId) 组合不重复。",
                StandardAnswer = "对",
                OptionsJson = null,
                KnowledgeTags = "数据库,索引",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.ShortAnswer,
                Difficulty = DifficultyLevel.Hard,
                Domain = QuestionDomain.DataStructure,
                Stem = "说明随机组卷时如何保证题型分布更均匀的一种策略。",
                StandardAnswer = "分层抽样",
                OptionsJson = null,
                KnowledgeTags = "考试引擎,算法",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Medium,
                Domain = QuestionDomain.Uncategorized,
                Stem = "错题本闭环中，最关键的业务动作是？",
                StandardAnswer = "A",
                OptionsJson = Opt("A. 答错自动归集并可重复练习", "B. 删除所有历史记录", "C. 禁止查看解析", "D. 仅展示排行榜"),
                KnowledgeTags = "业务,错题本",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Easy,
                Domain = QuestionDomain.Uncategorized,
                Stem = "HTTP 调用失败时，占位 AI 服务应优先？",
                StandardAnswer = "B",
                OptionsJson = Opt("A. 直接崩溃", "B. 返回可解释的降级结果", "C. 无限重试不提示", "D. 写入注册表"),
                KnowledgeTags = "AI,可靠性",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Hard,
                Domain = QuestionDomain.CSharp,
                Stem = "在 MVVM 中，视图层不应直接访问？",
                StandardAnswer = "C",
                OptionsJson = Opt("A. 资源字典", "B. 样式", "C. DbContext（建议通过服务注入）", "D. 画刷"),
                KnowledgeTags = "MVVM,分层",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.ShortAnswer,
                Difficulty = DifficultyLevel.Easy,
                Domain = QuestionDomain.CSharp,
                Stem = "写出一种计时倒计时的 UI 更新方式（关键词即可）。",
                StandardAnswer = "DispatcherTimer",
                OptionsJson = null,
                KnowledgeTags = "WPF,计时",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.MultipleChoice,
                Difficulty = DifficultyLevel.Hard,
                Domain = QuestionDomain.Uncategorized,
                Stem = "以下哪些属于 AI 刷题系统的合理能力边界？（多选）",
                StandardAnswer = "A,D",
                OptionsJson = Opt("A. 解析错题原因（可结构化）", "B. 替用户自动点击交卷", "C. 绕过本地数据库校验", "D. 推荐相似知识点题目"),
                KnowledgeTags = "AI,产品",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Medium,
                Domain = QuestionDomain.DataStructure,
                Stem = "交卷自动判分的关键输入是？",
                StandardAnswer = "A",
                OptionsJson = Opt("A. 用户答案与标准答案", "B. 窗口标题", "C. 主题色", "D. 鼠标 DPI"),
                KnowledgeTags = "考试引擎,判分",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.TrueFalse,
                Difficulty = DifficultyLevel.Easy,
                Domain = QuestionDomain.CSharp,
                Stem = "XML 文档注释会影响运行时性能。",
                StandardAnswer = "错",
                OptionsJson = null,
                KnowledgeTags = "C#,工程规范",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.SingleChoice,
                Difficulty = DifficultyLevel.Medium,
                Domain = QuestionDomain.Database,
                Stem = "连接字符串应主要存放于？",
                StandardAnswer = "B",
                OptionsJson = Opt("A. 源代码常量硬编码", "B. 配置文件（不入库密钥）", "C. 图片资源", "D. 注释里"),
                KnowledgeTags = "配置,安全",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            },
            new()
            {
                Type = QuestionType.ShortAnswer,
                Difficulty = DifficultyLevel.Medium,
                Domain = QuestionDomain.Database,
                Stem = "说明为何错题本表需要对 (UserId, QuestionId) 建唯一约束。",
                StandardAnswer = "去重",
                OptionsJson = null,
                KnowledgeTags = "数据库,错题本",
                IsEnabled = true,
                CreatedAtUtc = nowUtc
            }
        };
    }
}
