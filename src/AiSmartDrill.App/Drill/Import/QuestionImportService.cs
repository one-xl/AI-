using System.IO;
using System.Text.Json;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Import;

/// <summary>
/// 题库导入服务，支持从 JSON 文件批量导入题目。
/// </summary>
public sealed class QuestionImportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<QuestionImportService> _logger;

    /// <summary>
    /// 初始化 <see cref="QuestionImportService"/> 的新实例。
    /// </summary>
    /// <param name="dbFactory">数据库上下文工厂。</param>
    /// <param name="logger">日志记录器。</param>
    public QuestionImportService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<QuestionImportService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// 从 JSON 文件导入题目。
    /// </summary>
    /// <param name="filePath">JSON 文件路径。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>导入结果，包含成功数量和失败数量。</returns>
    public async Task<ImportResult> ImportFromJsonFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始导入题库文件：{FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogError("文件不存在：{FilePath}", filePath);
            return new ImportResult { SuccessCount = 0, FailCount = 0, Errors = new[] { "文件不存在" } };
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            var importDtos = JsonSerializer.Deserialize<List<QuestionImportDto>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (importDtos == null || importDtos.Count == 0)
            {
                _logger.LogWarning("导入文件为空或格式错误");
                return new ImportResult { SuccessCount = 0, FailCount = 0, Errors = new[] { "文件内容为空或格式错误" } };
            }

            return await ImportQuestionsAsync(importDtos, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON 解析失败");
            return new ImportResult { SuccessCount = 0, FailCount = 1, Errors = new[] { $"JSON 格式错误：{ex.Message}" } };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入过程发生异常");
            return new ImportResult { SuccessCount = 0, FailCount = 1, Errors = new[] { $"导入失败：{ex.Message}" } };
        }
    }

    /// <summary>
    /// 批量导入题目到数据库。
    /// </summary>
    private async Task<ImportResult> ImportQuestionsAsync(
        List<QuestionImportDto> importDtos,
        CancellationToken cancellationToken)
    {
        var result = new ImportResult();
        var errors = new List<string>();
        var validQuestions = new List<Question>();

        for (var i = 0; i < importDtos.Count; i++)
        {
            var dto = importDtos[i];
            var lineNumber = i + 1;

            try
            {
                var validationError = ValidateImportDto(dto);
                if (!string.IsNullOrEmpty(validationError))
                {
                    errors.Add($"第 {lineNumber} 题：{validationError}");
                    continue;
                }

                var question = ConvertToQuestion(dto);
                validQuestions.Add(question);
            }
            catch (Exception ex)
            {
                errors.Add($"第 {lineNumber} 题：处理失败 - {ex.Message}");
            }
        }

        if (validQuestions.Count > 0)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.Questions.AddRange(validQuestions);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("成功导入 {Count} 道题目", validQuestions.Count);
        }

        result.SuccessCount = validQuestions.Count;
        result.FailCount = errors.Count;
        result.Errors = errors;

        return result;
    }

    /// <summary>
    /// 验证导入 DTO 的必填字段。
    /// </summary>
    private string? ValidateImportDto(QuestionImportDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Stem))
            return "题干不能为空";

        if (string.IsNullOrWhiteSpace(dto.StandardAnswer))
            return "标准答案不能为空";

        if (string.IsNullOrWhiteSpace(dto.Type))
            return "题型不能为空";

        if (!Enum.TryParse<QuestionType>(dto.Type, true, out _))
            return $"无效的题型：{dto.Type}，有效值为：SingleChoice, MultipleChoice, TrueFalse, ShortAnswer, FillInBlank";

        if (string.IsNullOrWhiteSpace(dto.Difficulty))
            return "难度不能为空";

        if (!Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out _))
            return $"无效的难度：{dto.Difficulty}，有效值为：Easy, Medium, Hard";

        return null;
    }

    /// <summary>
    /// 将导入 DTO 转换为 Question 实体。
    /// </summary>
    private Question ConvertToQuestion(QuestionImportDto dto)
    {
        var type = Enum.Parse<QuestionType>(dto.Type!, true);
        var difficulty = Enum.Parse<DifficultyLevel>(dto.Difficulty!, true);

        return new Question
        {
            Type = type,
            Difficulty = difficulty,
            Stem = dto.Stem!.Trim(),
            StandardAnswer = dto.StandardAnswer!.Trim(),
            OptionsJson = string.IsNullOrWhiteSpace(dto.OptionsJson) ? null : dto.OptionsJson.Trim(),
            KnowledgeTags = string.IsNullOrWhiteSpace(dto.KnowledgeTags) ? "未分类" : dto.KnowledgeTags.Trim(),
            TopicTags = string.IsNullOrWhiteSpace(dto.TopicTags) ? string.Empty : dto.TopicTags.Trim(),
            TopicKeywords = string.IsNullOrWhiteSpace(dto.TopicKeywords) ? string.Empty : dto.TopicKeywords.Trim(),
            IsEnabled = dto.IsEnabled ?? true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

/// <summary>
/// 题目导入 DTO，用于 JSON 反序列化。
/// </summary>
public sealed class QuestionImportDto
{
    public string? Type { get; init; }
    public string? Difficulty { get; init; }
    public string? Stem { get; init; }
    public string? StandardAnswer { get; init; }
    public string? OptionsJson { get; init; }
    public string? KnowledgeTags { get; init; }
    public string? TopicTags { get; init; }
    public string? TopicKeywords { get; init; }
    public bool? IsEnabled { get; init; }
}

/// <summary>
/// 导入结果。
/// </summary>
public sealed class ImportResult
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
