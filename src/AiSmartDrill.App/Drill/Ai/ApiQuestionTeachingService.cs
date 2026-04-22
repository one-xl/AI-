using System.Text;
using System.Text.Json;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Drill.Ai.Ark;
using AiSmartDrill.App.Drill.Ai.Client;
using AiSmartDrill.App.Drill.Import;
using AiSmartDrill.App.Infrastructure;
using Microsoft.EntityFrameworkCore;
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
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ApiQuestionTeachingService> _logger;
    private readonly AiCallTrace _trace;

    /// <summary>
    /// 初始化 <see cref="ApiQuestionTeachingService"/>。
    /// </summary>
    /// <param name="chatCompletionService">方舟兼容聊天完成客户端。</param>
    /// <param name="dbFactory">题库上下文工厂（读取已有知识点短语表）。</param>
    /// <param name="logger">日志记录器。</param>
    /// <param name="trace">AI 调用轨迹（供 UI 区分云端/回退）。</param>
    public ApiQuestionTeachingService(
        IChatCompletionService chatCompletionService,
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<ApiQuestionTeachingService> logger,
        AiCallTrace trace)
    {
        _chat = chatCompletionService;
        _dbFactory = dbFactory;
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
        var randomDiff = hints?.RandomizeDifficultyInBatch == true;
        var lockedDiff = !randomDiff && hints?.RequiredDifficulty is { } ld ? ld : (DifficultyLevel?)null;
        _logger.LogInformation(
            "AI 题库生成：Domain={Domain}, TemplateType={Type}, Count={Count}, LockedDifficulty={LockDiff}, RandomizeDifficulty={Rand}",
            domain,
            templateType,
            n,
            lockedDiff,
            randomDiff);

        var typeToken = templateType.ToString();
        var typeChinese = MapTypeToChinese(templateType);

        var system =
            "你是资深专业题库出题人。必须只输出一个 JSON 数组，不要 Markdown 围栏，不要任何说明文字。\n" +
            "数组每一项对象字段（英文字段名）：\n" +
            "- Type(string)：必须是 " + typeToken + "（不得改变）。\n" +
            "- Difficulty(string)：Easy / Medium / Hard 之一。\n" +
            "- Stem(string)：题干，中文，避免泄露答案。\n" +
            "- StandardAnswer(string)：客观题用选项键（单选如 A；多选如 A,C；判断题用 对 或 错）；简答题用分号分隔关键词；填空题用合法正则表达式。\n" +
            "- OptionsJson(string 或 null)：若为单选/多选，必须是 JSON 数组的字符串形式，例如 [\"选项A文本\",\"选项B文本\"]（至少 2 项）；判断/简答/填空可 null。\n" +
            "- KnowledgeTags(string)：逗号分隔细知识点标签；下一条用户消息会给出「当前选中领域下、按频次排序的题库已有知识点」列表时，须优先整段复用列表中的短语（字面一致或明显同义可视为复用）；仅当列表中确无合适项时才创造新短语，新短语须为该领域内的专业考查点表述，禁止混入其他学科/其他编程语言体系作为主要标签。\n" +
            "- PrimaryKnowledgePoint(string 或 null)：须为 KnowledgeTags 中某一项的原文；可省略（系统会取 KnowledgeTags 首项）。\n" +
            "- TopicTags(string)：逗号分隔分类标签（须体现用户指定领域下的技术细分，如语法/并发/IO/网络协议/SQL 等），用于推荐筛选。\n" +
            "- TopicKeywords(string)：分号或逗号分隔检索关键词，须与题干考查的技术主题一致。\n" +
            "【领域纪律】用户会在下一条消息给出领域。所有题目的考查点必须落在该领域专业知识内；严禁把日常生活、购物、娱乐明星、体育比赛、社会新闻、家庭故事等作为题干主体或主要考查对象（最多允许一句极短类比，且设问仍须考专业点）。若用户领域为「未分类」，仍只出计算机与信息技术相关题，禁止纯生活常识题。";

        if (randomDiff)
        {
            system += n <= 1
                ? "\n【难度随机】本批仅 1 题：Difficulty 须在 Easy、Medium、Hard 中随机选一。"
                : "\n【难度随机】本批共 " + n +
                  " 题：每一条的 Difficulty 字段须独立在 Easy、Medium、Hard 三者中随机选取；" +
                  (n >= 3
                      ? "同一数组内三种难度都应尽量出现，分布大致均衡；禁止全部题目使用同一难度值。"
                      : "尽量使各题难度不完全相同。");
        }
        else if (lockedDiff is DifficultyLevel locked)
        {
            system += "\n本次输出中每一条题目的 Difficulty 字段必须为 " + locked +
                      "（英文枚举名，与 Easy/Medium/Hard 对应），不得使用其他难度。";
        }

        var userBuilder = new StringBuilder();
        userBuilder.AppendLine("领域（中文显示名）：" + domainDisplayName);
        userBuilder.AppendLine("领域枚举（须与之一致，用于你对范围的自我校验）：" + domain + " → " + MapDomainToChinese(domain) + "。");
        userBuilder.AppendLine(BuildDomainLockUserInstructions(domain, domainDisplayName));
        userBuilder.AppendLine("题型模板：" + typeChinese + "（Type 字段固定为 " + typeToken + "）");
        userBuilder.AppendLine("请生成 " + n + " 道互不重复、表述清晰的题目。");

        if (randomDiff)
        {
            userBuilder.AppendLine(
                n <= 1
                    ? "难度要求：本题 Difficulty 在 Easy、Medium、Hard 中随机选一。"
                    : n >= 3
                        ? "难度要求：本批 " + n +
                          " 题请各题 Difficulty 在 Easy、Medium、Hard 间随机且整批尽量均衡，不得整批均为同一难度。"
                        : "难度要求：本批 " + n + " 题请各题 Difficulty 在 Easy、Medium、Hard 间随机，尽量避免题题相同。");
        }
        else if (lockedDiff is DifficultyLevel req)
        {
            userBuilder.AppendLine("必须难度：Difficulty 字段只能为 " + req + "。");
        }

        if (!string.IsNullOrWhiteSpace(hints?.TopicTagsHint))
        {
            userBuilder.AppendLine("TopicTags 须覆盖或紧密关联以下分类要点：" + hints.TopicTagsHint.Trim());
        }

        if (!string.IsNullOrWhiteSpace(hints?.KnowledgeTagsHint))
        {
            userBuilder.AppendLine("KnowledgeTags 与 PrimaryKnowledgePoint 须优先围绕以下知识点短语展开：" + hints.KnowledgeTagsHint.Trim());
        }

        if (!string.IsNullOrWhiteSpace(hints?.TopicKeywordsHint))
        {
            userBuilder.AppendLine("题干与 TopicKeywords 须贴近以下关键词方向：" + hints.TopicKeywordsHint.Trim());
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        const int catalogAlignPool = 200;
        var fullKpCatalog = await KnowledgePointCatalogQuery.LoadOrderedByFrequencyAsync(
                db,
                domain,
                catalogAlignPool,
                cancellationToken)
            .ConfigureAwait(false);
        var kpCatalogForPrompt = fullKpCatalog.Count <= KnowledgePointCatalogQuery.DefaultPromptCatalogLimit
            ? fullKpCatalog
            : fullKpCatalog.Take(KnowledgePointCatalogQuery.DefaultPromptCatalogLimit).ToList();
        if (kpCatalogForPrompt.Count > 0)
        {
            userBuilder.AppendLine(
                "【题库已有知识点短语（仅当前选中领域 " + domain + " / " + domainDisplayName +
                "，按频次排序；请优先在 KnowledgeTags 中整段复用；无合适项再创新，新标签须明显属于该领域）】" +
                string.Join("、", kpCatalogForPrompt));
        }
        else
        {
            userBuilder.AppendLine(
                "【题库已有知识点短语】当前领域暂无已入库记录，可自行命名新知识点，须为该领域专业考查点且与题干强相关。");
        }

        var user = userBuilder.ToString().TrimEnd();

        var messages = new List<ChatMessage>
        {
            new() { Role = "system", Content = system },
            new() { Role = "user", Content = user }
        };

        var response = await _chat
            .GenerateCompletionAsync(messages, null, AiCompletionTokenBudgets.BankGeneration(n), cancellationToken)
            .ConfigureAwait(false);
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
            var err = ValidateGeneratedDto(dto, templateType, line, randomDiff ? null : hints?.RequiredDifficulty);
            if (err is not null)
            {
                errors.Add(err);
                continue;
            }

            var q = ConvertDtoToQuestion(dto!, domain);
            KnowledgePointCatalogQuery.AlignQuestionKnowledgeFields(q, fullKpCatalog);
            ok.Add(q);
        }

        _trace.Set(errors.Count == 0 && ok.Count > 0 ? "bank-gen:ark" : "bank-gen:ark+partial", true);
        return new QuestionBankAiGenerationResult(ok, errors, TrimSnippet(raw));
    }

    /// <inheritdoc />
    public async Task<ExamQuestionExplainResult> ExplainQuestionAsync(
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
                    "你是耐心讲师。用中文输出，不要问候或自称，不要用 Markdown 代码围栏。\n" +
                    "必须严格包含且仅使用以下两行作为段标题（不要加 # 号）：\n" +
                    "【结论】\n" +
                    "下一行起：用不超过 25 个汉字给出可判分的要点（客观题写正确选项键如 A 或 A,C，判断题写 对/错，简答写核心关键词短语）；不要写推理过程。\n" +
                    "【详解】\n" +
                    "下一行起：教学向展开，约 280～380 字：考查点一句；正确思路 2～4 短句；易错提醒一句（可省略）。"
            },
            new() { Role = "user", Content = userPayload.ToString() }
        };

        var response = await _chat
            .GenerateCompletionAsync(messages, null, AiCompletionTokenBudgets.ExamExplain, cancellationToken)
            .ConfigureAwait(false);
        var text = ArkAssistantReply.GetPrimaryText(response).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            _trace.Set("exam-explain:empty", false);
            throw new InvalidOperationException("模型返回内容为空。");
        }

        _trace.Set("exam-explain:ark", true);
        return ExamQuestionExplainResult.ParseFromModelOutput(text);
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
            PrimaryKnowledgePoint = string.IsNullOrWhiteSpace(dto.PrimaryKnowledgePoint)
                ? string.Empty
                : dto.PrimaryKnowledgePoint.Trim(),
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

    /// <summary>
    /// 构造写入用户消息的「领域锁」说明，减少模型输出日常生活类跑题题。
    /// </summary>
    /// <param name="domain">当前选择的领域枚举。</param>
    /// <param name="domainDisplayName">界面上的领域中文名（可能与枚举略不同）。</param>
    private static string BuildDomainLockUserInstructions(QuestionDomain domain, string? domainDisplayName)
    {
        var display = string.IsNullOrWhiteSpace(domainDisplayName) ? "当前领域" : domainDisplayName.Trim();
        var sb = new StringBuilder();
        sb.AppendLine("【领域硬性要求】");
        sb.AppendLine("- 题干、选项与标准答案所考查的知识点必须落在「" + display + "」相关的专业技术范围内（教材、认证考点或常见工程问题）。");
        sb.AppendLine("- 禁止以下类型作为题干主体或主要考查对象：日常生活琐事、购物消费、家庭人际、明星综艺、体育比赛、社会新闻与八卦、纯作文式感悟等与信息技术职业能力无关的内容。");
        sb.AppendLine("- 若需要降低理解门槛，最多用一句极简类比，但设问与判分点仍必须是上述专业技术点，不得让生活故事成为题目核心。");
        if (domain == QuestionDomain.Uncategorized)
        {
            sb.AppendLine("- 当前领域标签偏宽：请只出计算机与信息技术范畴内的通用题（如数据结构、网络、操作系统、数据库、编程语言机制等），仍禁止纯生活常识题。");
        }

        return sb.ToString().TrimEnd();
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
