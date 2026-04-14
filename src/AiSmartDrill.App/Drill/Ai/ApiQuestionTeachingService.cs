using System.Text;
using System.Text.Json;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Drill.Ai.Ark;
using AiSmartDrill.App.Drill.Ai.Client;
using AiSmartDrill.App.Drill.Import;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 面向「题库 AI 生成」与「考试中单题讲解」的方舟调用实现：统一走
/// <see cref="IChatCompletionService"/>，并在成功后更新 <see cref="AiCallTrace"/>。
/// </summary>
public sealed class ApiQuestionTeachingService : IQuestionBankAiGenerationService, IExamQuestionAiExplainService
{
    private const int MaxGenerationCount = 12;
    private const int RawSnippetMaxLen = 800;

    private readonly IChatCompletionService _chat;
    private readonly ILogger<ApiQuestionTeachingService> _logger;
    private readonly AiCallTrace _trace;

    /// <summary>
    /// 初始化 <see cref="ApiQuestionTeachingService"/>。
    /// </summary>
    /// <param name="chatCompletionService">方舟兼容聊天完成客户端。</param>
    /// <param name="logger">日志记录器。</param>
    /// <param name="trace">AI 调用轨迹（供 UI 区分云端/回退）。</param>
    public ApiQuestionTeachingService(
        IChatCompletionService chatCompletionService,
        ILogger<ApiQuestionTeachingService> logger,
        AiCallTrace trace)
    {
        _chat = chatCompletionService;
        _logger = logger;
        _trace = trace;
    }

    /// <inheritdoc />
    public async Task<QuestionBankAiGenerationResult> GenerateQuestionsAsync(
        QuestionDomain domain,
        string domainDisplayName,
        QuestionType templateType,
        int count,
        QuestionBankGenerationHints? hints = null,
        CancellationToken cancellationToken = default)
    {
        var n = Math.Clamp(count, 1, MaxGenerationCount);
        _logger.LogInformation(
            "AI 题库生成：Domain={Domain}, TemplateType={Type}, Count={Count}, LockedDifficulty={LockDiff}",
            domain,
            templateType,
            n,
            hints?.RequiredDifficulty);

        var typeToken = templateType.ToString();
        var typeChinese = MapTypeToChinese(templateType);

        var system =
            "你是资深出题人。必须只输出一个 JSON 数组，不要 Markdown 围栏，不要任何说明文字。\n" +
            "数组每一项对象字段（英文字段名）：\n" +
            "- Type(string)：必须是 " + typeToken + "（不得改变）。\n" +
            "- Difficulty(string)：Easy / Medium / Hard 之一。\n" +
            "- Stem(string)：题干，中文，避免泄露答案。\n" +
            "- StandardAnswer(string)：客观题用选项键（单选如 A；多选如 A,C；判断题用 对 或 错）；简答题用分号分隔关键词；填空题用合法正则表达式。\n" +
            "- OptionsJson(string 或 null)：若为单选/多选，必须是 JSON 数组的字符串形式，例如 [\"选项A文本\",\"选项B文本\"]（至少 2 项）；判断/简答/填空可 null。\n" +
            "- KnowledgeTags(string)：逗号分隔知识点标签。\n" +
            "- TopicTags(string)：逗号分隔分类标签（可含「领域/层级」如 Python/基础，并组合概念/应用/语法/API/并发/数据结构/SQL 等细分词），用于推荐筛选。\n" +
            "- TopicKeywords(string)：分号或逗号分隔检索关键词，须与题干主题一致。\n" +
            "题目内容必须贴合用户给出的「领域」语境，且难度分布合理。";

        if (hints?.RequiredDifficulty is DifficultyLevel locked)
        {
            system += "\n本次输出中每一条题目的 Difficulty 字段必须为 " + locked +
                      "（英文枚举名，与 Easy/Medium/Hard 对应），不得使用其他难度。";
        }

        var userBuilder = new StringBuilder();
        userBuilder.AppendLine("领域（中文显示名）：" + domainDisplayName);
        userBuilder.AppendLine("题型模板：" + typeChinese + "（Type 字段固定为 " + typeToken + "）");
        userBuilder.AppendLine("请生成 " + n + " 道互不重复、表述清晰的题目。");

        if (hints?.RequiredDifficulty is DifficultyLevel req)
        {
            userBuilder.AppendLine("必须难度：Difficulty 字段只能为 " + req + "。");
        }

        if (!string.IsNullOrWhiteSpace(hints?.TopicTagsHint))
        {
            userBuilder.AppendLine("TopicTags 须覆盖或紧密关联以下分类要点：" + hints.TopicTagsHint.Trim());
        }

        if (!string.IsNullOrWhiteSpace(hints?.TopicKeywordsHint))
        {
            userBuilder.AppendLine("题干与 TopicKeywords 须贴近以下关键词方向：" + hints.TopicKeywordsHint.Trim());
        }

        var user = userBuilder.ToString().TrimEnd();

        var messages = new List<ChatMessage>
        {
            new() { Role = "system", Content = system },
            new() { Role = "user", Content = user }
        };

        var response = await _chat.GenerateCompletionAsync(messages, null, cancellationToken).ConfigureAwait(false);
        var raw = ArkAssistantReply.GetPrimaryText(response);
        var json = ArkModelOutputParsing.ExtractFirstJsonValue(raw);
        if (string.IsNullOrWhiteSpace(json))
        {
            _trace.Set("bank-gen:empty-json", false);
            throw new InvalidOperationException("模型未返回可解析的 JSON。");
        }

        List<QuestionImportDto>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<QuestionImportDto>>(json, ArkChatJsonDefaults.ModelPayloadOptions);
        }
        catch (JsonException ex)
        {
            _trace.Set("bank-gen:json-parse", false);
            throw new InvalidOperationException("JSON 反序列化失败：" + ex.Message);
        }

        if (parsed is null || parsed.Count == 0)
        {
            _trace.Set("bank-gen:no-items", false);
            throw new InvalidOperationException("JSON 数组为空。");
        }

        var errors = new List<string>();
        var ok = new List<Question>();
        for (var i = 0; i < parsed.Count; i++)
        {
            var dto = parsed[i];
            var line = i + 1;
            var err = ValidateGeneratedDto(dto, templateType, line, hints?.RequiredDifficulty);
            if (err is not null)
            {
                errors.Add(err);
                continue;
            }

            ok.Add(ConvertDtoToQuestion(dto!, domain));
        }

        _trace.Set(errors.Count == 0 && ok.Count > 0 ? "bank-gen:ark" : "bank-gen:ark+partial", true);
        return new QuestionBankAiGenerationResult(ok, errors, TrimSnippet(raw));
    }

    /// <inheritdoc />
    public async Task<string> ExplainQuestionAsync(
        Question question,
        string? userAnswerSnapshot,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI 单题讲解：QuestionId={Id}, Type={Type}", question.Id, question.Type);

        var domainText = MapDomainToChinese(question.Domain);
        var typeText = MapTypeToChinese(question.Type);
        var diffText = MapDifficultyToChinese(question.Difficulty);
        var opts = string.IsNullOrWhiteSpace(question.OptionsJson) ? "（无选项 JSON）" : question.OptionsJson;

        var userPayload = new StringBuilder();
        userPayload.AppendLine("领域：" + domainText);
        userPayload.AppendLine("题型：" + typeText);
        userPayload.AppendLine("难度：" + diffText);
        userPayload.AppendLine("题干：");
        userPayload.AppendLine(question.Stem);
        userPayload.AppendLine("选项 JSON 原文：");
        userPayload.AppendLine(opts);
        userPayload.AppendLine("学习者当前作答（可能为空）：");
        userPayload.AppendLine(string.IsNullOrWhiteSpace(userAnswerSnapshot) ? "（空）" : userAnswerSnapshot.Trim());
        userPayload.AppendLine("标准答案（供你组织讲解，请用教学口吻，不要简单复读标答一行了事）：");
        userPayload.AppendLine(question.StandardAnswer);

        var messages = new List<ChatMessage>
        {
            new()
            {
                Role = "system",
                Content =
                    "你是耐心讲师。请用中文输出一段完整解析：先点出考查点，再给出正确思路，必要时给出易错提醒。" +
                    "不要输出问候语或自我称呼。不要使用 Markdown 代码围栏。"
            },
            new() { Role = "user", Content = userPayload.ToString() }
        };

        var response = await _chat.GenerateCompletionAsync(messages, null, cancellationToken).ConfigureAwait(false);
        var text = ArkAssistantReply.GetPrimaryText(response).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            _trace.Set("exam-explain:empty", false);
            throw new InvalidOperationException("模型返回内容为空。");
        }

        _trace.Set("exam-explain:ark", true);
        return text;
    }

    /// <summary>
    /// 校验模型返回的一条题目是否符合题型模板与客观题选项约束。
    /// </summary>
    private static string? ValidateGeneratedDto(
        QuestionImportDto? dto,
        QuestionType expectedType,
        int line,
        DifficultyLevel? requiredDifficulty = null)
    {
        if (dto is null)
        {
            return $"第 {line} 条：对象为 null。";
        }

        if (string.IsNullOrWhiteSpace(dto.Stem))
        {
            return $"第 {line} 条：题干不能为空。";
        }

        if (string.IsNullOrWhiteSpace(dto.StandardAnswer))
        {
            return $"第 {line} 条：标准答案不能为空。";
        }

        if (string.IsNullOrWhiteSpace(dto.Type) || !Enum.TryParse<QuestionType>(dto.Type, true, out var parsedType))
        {
            return $"第 {line} 条：题型无效：{dto.Type}";
        }

        if (parsedType != expectedType)
        {
            return $"第 {line} 条：题型必须为模板指定值 {expectedType}，实际为 {parsedType}。";
        }

        if (string.IsNullOrWhiteSpace(dto.Difficulty) || !Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var diffLevel))
        {
            return $"第 {line} 条：难度无效：{dto.Difficulty}";
        }

        if (requiredDifficulty is DifficultyLevel req && diffLevel != req)
        {
            return $"第 {line} 条：难度必须为 {req}，实际为 {diffLevel}。";
        }

        switch (expectedType)
        {
            case QuestionType.SingleChoice:
            case QuestionType.MultipleChoice:
            {
                var optErr = ValidateOptionsJsonArray(dto.OptionsJson, line);
                if (optErr is not null)
                {
                    return optErr;
                }

                break;
            }
            case QuestionType.TrueFalse:
                // 允许 null；若提供则仍校验为合法 JSON 数组
                if (!string.IsNullOrWhiteSpace(dto.OptionsJson))
                {
                    var optErr = ValidateOptionsJsonArray(dto.OptionsJson, line);
                    if (optErr is not null)
                    {
                        return optErr;
                    }
                }

                break;
        }

        return null;
    }

    /// <summary>
    /// 校验 <c>OptionsJson</c> 是否为至少包含两项的 JSON 字符串数组。
    /// </summary>
    private static string? ValidateOptionsJsonArray(string? optionsJson, int line)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return $"第 {line} 条：客观题缺少 OptionsJson。";
        }

        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(optionsJson, ArkChatJsonDefaults.ModelPayloadOptions);
            if (arr is null || arr.Count < 2)
            {
                return $"第 {line} 条：OptionsJson 至少需要 2 个选项。";
            }
        }
        catch (JsonException)
        {
            return $"第 {line} 条：OptionsJson 不是合法 JSON 数组字符串。";
        }

        return null;
    }

    /// <summary>
    /// 将已通过校验的导入 DTO 转为实体并写入领域与时间戳。
    /// </summary>
    private static Question ConvertDtoToQuestion(QuestionImportDto dto, QuestionDomain domain)
    {
        var type = Enum.Parse<QuestionType>(dto.Type!, true);
        var difficulty = Enum.Parse<DifficultyLevel>(dto.Difficulty!, true);

        return new Question
        {
            Type = type,
            Difficulty = difficulty,
            Domain = domain,
            Stem = dto.Stem!.Trim(),
            StandardAnswer = dto.StandardAnswer!.Trim(),
            OptionsJson = string.IsNullOrWhiteSpace(dto.OptionsJson) ? null : dto.OptionsJson.Trim(),
            KnowledgeTags = string.IsNullOrWhiteSpace(dto.KnowledgeTags) ? "AI生成" : dto.KnowledgeTags.Trim(),
            TopicTags = string.IsNullOrWhiteSpace(dto.TopicTags) ? "AI生成" : dto.TopicTags.Trim(),
            TopicKeywords = string.IsNullOrWhiteSpace(dto.TopicKeywords) ? string.Empty : dto.TopicKeywords.Trim(),
            IsEnabled = dto.IsEnabled ?? true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static string TrimSnippet(string raw)
    {
        var t = raw.Trim();
        return t.Length <= RawSnippetMaxLen ? t : t[..RawSnippetMaxLen] + "…";
    }

    private static string MapTypeToChinese(QuestionType t) => t switch
    {
        QuestionType.MultipleChoice => "多选题",
        QuestionType.TrueFalse => "判断题",
        QuestionType.ShortAnswer => "简答题",
        QuestionType.FillInBlank => "填空题（标答为正则）",
        _ => "单选题"
    };

    private static string MapDifficultyToChinese(DifficultyLevel d) => d switch
    {
        DifficultyLevel.Medium => "中等",
        DifficultyLevel.Hard => "困难",
        _ => "简单"
    };

    private static string MapDomainToChinese(QuestionDomain d) => d switch
    {
        QuestionDomain.Python => "Python",
        QuestionDomain.C => "C",
        QuestionDomain.CPlusPlus => "C++",
        QuestionDomain.CSharp => "C#",
        QuestionDomain.Rust => "Rust",
        QuestionDomain.Java => "Java",
        QuestionDomain.JavaScript => "JavaScript",
        QuestionDomain.Go => "Go",
        QuestionDomain.DataStructure => "数据结构与算法",
        QuestionDomain.Database => "数据库",
        QuestionDomain.OperatingSystem => "操作系统",
        QuestionDomain.ComputerNetwork => "计算机网络",
        _ => "未分类"
    };
}
