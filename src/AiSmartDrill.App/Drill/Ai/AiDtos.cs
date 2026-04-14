using System.Text;
using System.Text.Json;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Drill.Ai.Ark;

namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 表示一道错题的 AI 解析结果，用于成绩页展示与导出。
/// </summary>
public sealed class WrongQuestionInsightDto
{
    /// <summary>
    /// 获取或设置题目主键。
    /// </summary>
    public long QuestionId { get; init; }

    /// <summary>
    /// 获取或设置题型。
    /// </summary>
    public QuestionType Type { get; init; }

    /// <summary>
    /// 获取或设置题干摘要（可截断，用于列表等）。
    /// </summary>
    public string StemSummary { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置完整题干；发送给模型时优先于此，避免摘要截断导致解析笼统。
    /// </summary>
    public string StemFull { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置客观题选项 JSON（字符串数组）；简答/填空等可为 null。
    /// </summary>
    public string? OptionsJson { get; init; }

    /// <summary>
    /// 获取或设置知识点标签（逗号分隔），辅助模型定位考点。
    /// </summary>
    public string? KnowledgeTags { get; init; }

    /// <summary>
    /// 获取或设置用户作答。
    /// </summary>
    public string UserAnswer { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置标准答案。
    /// </summary>
    public string StandardAnswer { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置错误原因分析（自然语言）。
    /// </summary>
    public string RootCause { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置解题思路（分步建议）。
    /// </summary>
    public string SolutionHints { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置逐选项辨析：对单选/多选等须说明各选项正误与技术理由；无选项题型则说明得分点与表述要求。
    /// </summary>
    public string OptionAnalysis { get; init; } = string.Empty;
}

/// <summary>
/// 在 Ark 不可用或模型漏字段时，为错题解析拼装简短的逐选项/逐要点对照文案。
/// </summary>
public static class WrongQuestionInsightTextFallback
{
    /// <summary>
    /// 根据题型、选项 JSON 与用户/标准答案生成回退用的「选项辨析」段落。
    /// </summary>
    public static string BuildOptionAnalysis(WrongQuestionInsightDto item)
    {
        if (item.Type == QuestionType.TrueFalse)
        {
            return
                $"本题为判断题：标准结论为「{item.StandardAnswer}」，你的作答为「{item.UserAnswer}」。请回到教材或定义中的判定条件，逐条核对命题是否成立，避免绝对化表述误判。";
        }

        if (item.Type is QuestionType.ShortAnswer or QuestionType.FillInBlank)
        {
            return
                $"本题无 A/B 类独立选项。请对照标准答案「{item.StandardAnswer}」：简答题抓关键词与要点顺序；填空题若标答为正则式，请检查你的表述是否命中得分模式。";
        }

        if (string.IsNullOrWhiteSpace(item.OptionsJson))
        {
            return "本题未附带选项文本 JSON。请仍将你的作答与标准答案逐项对齐，并回忆相关概念定义。";
        }

        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(item.OptionsJson, ArkChatJsonDefaults.ModelPayloadOptions);
            if (arr is null || arr.Count == 0)
            {
                return "选项 JSON 无法解析为列表，请直接对照标准答案与知识点复盘。";
            }

            var correct = ParseChoiceKeys(item.StandardAnswer);
            var user = ParseChoiceKeys(item.UserAnswer);
            var sb = new StringBuilder();
            sb.AppendLine("（以下为本地回退简要对照；联网解析会给出更细的辨析。）");
            for (var i = 0; i < arr.Count && i < 26; i++)
            {
                var key = ((char)('A' + i)).ToString();
                var inStd = correct.Contains(key);
                var picked = user.Contains(key);
                var body = arr[i].Trim();
                if (body.Length > 56)
                {
                    body = body[..56] + "…";
                }

                var role = inStd ? "属于标准答案" : "不属于标准答案";
                var pick = picked ? "你已选" : "你未选";
                sb.AppendLine($"{key}. {body} — {role}；{pick}。");
            }

            return sb.ToString().TrimEnd();
        }
        catch (JsonException)
        {
            return "选项 JSON 解析失败，请对照标准答案手动复盘各选项。";
        }
    }

    private static HashSet<string> ParseChoiceKeys(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return set;
        }

        foreach (var p in raw.Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (p.Length == 1 && char.IsLetter(p[0]))
            {
                set.Add(char.ToUpperInvariant(p[0]).ToString());
            }
            else
            {
                foreach (var c in p.Where(static ch => char.IsLetter(ch)))
                {
                    set.Add(char.ToUpperInvariant(c).ToString());
                }
            }
        }

        return set;
    }
}

/// <summary>
/// 调用 AI 题目推荐时的上下文：错题本勾选、领域范围与排除规则由 UI 传入。
/// </summary>
public sealed class QuestionRecommendationRequest
{
    /// <summary>
    /// 获取错题本中用户勾选的题目 Id；其 TopicTags/TopicKeywords 会作为 AI 的主要信号。
    /// </summary>
    public IReadOnlyList<long> SelectedWrongQuestionIds { get; init; } = Array.Empty<long>();

    /// <summary>
    /// 获取「错题本 / AI」页领域筛选：为 null 表示「全部」领域；非 null 时候选与错题统计均限定该领域。
    /// </summary>
    public QuestionDomain? DomainScope { get; init; }
}

/// <summary>
/// 表示 AI 题目推荐返回的候选题集合。
/// </summary>
public sealed class QuestionRecommendationDto
{
    /// <summary>
    /// 获取或设置推荐理由。
    /// </summary>
    public string Rationale { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置模型给出的聚焦分类标签（用于服务端二次筛选）。
    /// </summary>
    public IReadOnlyList<string> FocusTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置模型给出的聚焦关键词（用于服务端二次筛选）。
    /// </summary>
    public IReadOnlyList<string> FocusKeywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置推荐题目 Id 列表（来自题库）。
    /// </summary>
    public IReadOnlyList<long> RecommendedQuestionIds { get; init; } = Array.Empty<long>();
}

/// <summary>
/// 表示阶段性刷题计划（日程级），供学习计划页展示。
/// </summary>
public sealed class StudyPlanDto
{
    /// <summary>
    /// 获取或设置计划标题。
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置每日建议题量。
    /// </summary>
    public int DailyQuestionQuota { get; init; }

    /// <summary>
    /// 获取或设置重点知识点标签集合。
    /// </summary>
    public IReadOnlyList<string> FocusKnowledgeTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置阶段天数。
    /// </summary>
    public int PhaseDays { get; init; }

    /// <summary>
    /// 获取或设置计划说明（自然语言）。
    /// </summary>
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// 用于生成 AI 计划的输入摘要，聚合用户整体表现。
/// </summary>
public sealed class UserPerformanceSummary
{
    /// <summary>
    /// 获取或设置用户 Id。
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// 获取或设置总答题次数。
    /// </summary>
    public int TotalAttempts { get; init; }

    /// <summary>
    /// 获取或设置正确次数。
    /// </summary>
    public int CorrectAttempts { get; init; }

    /// <summary>
    /// 获取或设置错题条目数。
    /// </summary>
    public int WrongBookCount { get; init; }

    /// <summary>
    /// 获取或设置高频错误知识点标签（逗号分隔或列表）。
    /// </summary>
    public IReadOnlyList<string> WeakTags { get; init; } = Array.Empty<string>();
}
