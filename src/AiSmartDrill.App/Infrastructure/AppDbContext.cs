using AiSmartDrill.App.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiSmartDrill.App.Infrastructure;

/// <summary>
/// 应用程序 EF Core 数据库上下文，负责三大核心表的映射与索引配置。
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// 初始化 <see cref="AppDbContext"/> 的新实例。
    /// </summary>
    /// <param name="options">EF Core 配置选项。</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 获取用户集合。
    /// </summary>
    public DbSet<AppUser> Users => Set<AppUser>();

    /// <summary>
    /// 获取题库集合。
    /// </summary>
    public DbSet<Question> Questions => Set<Question>();

    /// <summary>
    /// 获取答题记录集合。
    /// </summary>
    public DbSet<AnswerRecord> AnswerRecords => Set<AnswerRecord>();

    /// <summary>
    /// 获取错题本集合。
    /// </summary>
    public DbSet<WrongBookEntry> WrongBookEntries => Set<WrongBookEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 用户表：DisplayName 长度约束，避免异常超长输入。
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("AppUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        });

        // 题库表：题干与答案字段长度、索引（题型+难度复合筛选）。
        modelBuilder.Entity<Question>(e =>
        {
            e.ToTable("Questions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Stem).HasMaxLength(4000).IsRequired();
            e.Property(x => x.StandardAnswer).HasMaxLength(2000).IsRequired();
            e.Property(x => x.OptionsJson).HasMaxLength(4000);
            e.Property(x => x.KnowledgeTags).HasMaxLength(512).IsRequired();
            e.HasIndex(x => new { x.Type, x.Difficulty, x.IsEnabled })
                .HasDatabaseName("IX_Questions_Type_Difficulty_Enabled");
        });

        // 答题记录表：外键与按用户/会话查询索引。
        modelBuilder.Entity<AnswerRecord>(e =>
        {
            e.ToTable("AnswerRecords");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserAnswer).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Score).HasPrecision(9, 2);
            e.HasIndex(x => new { x.UserId, x.SessionId }).HasDatabaseName("IX_AnswerRecords_User_Session");
            e.HasIndex(x => new { x.UserId, x.QuestionId }).HasDatabaseName("IX_AnswerRecords_User_Question");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Question)
                .WithMany(q => q.AnswerRecords)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 错题本表：用户+题目唯一，支撑去重与统计。
        modelBuilder.Entity<WrongBookEntry>(e =>
        {
            e.ToTable("WrongBookEntries");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.QuestionId })
                .IsUnique()
                .HasDatabaseName("UX_WrongBook_User_Question");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Question)
                .WithMany(q => q.WrongBookEntries)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
