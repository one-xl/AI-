using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Drill.Grading;

/// <summary>
/// 判分器：将用户答案与标准答案进行题型相关的规范化比较。
/// </summary>
public static class AnswerGrader
{
    /// <summary>
    /// 对用户答案进行基础规范化（去空白、统一大小写入口）。
    /// </summary>
    /// <param name="answer">原始答案。</param>
    /// <returns>规范化后的答案。</returns>
    public static string NormalizeWhitespace(string? answer)
    {
        return (answer ?? string.Empty).Trim();
    }

    /// <summary>
    /// 判断用户答案是否正确。
    /// </summary>
    /// <param name="question">题目实体。</param>
    /// <param name="userAnswer">用户作答。</param>
    /// <returns>是否正确。</returns>
    public static bool IsCorrect(Question question, string? userAnswer)
    {
        var ua = NormalizeWhitespace(userAnswer);
        var sa = NormalizeWhitespace(question.StandardAnswer);

        return question.Type switch
        {
            QuestionType.TrueFalse => GradeTrueFalse(ua, sa),
            QuestionType.MultipleChoice => GradeMultipleChoice(ua, sa),
            QuestionType.ShortAnswer => GradeShortAnswer(ua, sa),
            _ => GradeSingleChoice(ua, sa)
        };
    }

    /// <summary>
    /// 单选题判分：忽略大小写。
    /// </summary>
    /// <param name="userAnswer">用户答案。</param>
    /// <param name="standardAnswer">标准答案。</param>
    /// <returns>是否正确。</returns>
    private static bool GradeSingleChoice(string userAnswer, string standardAnswer)
    {
        return string.Equals(userAnswer, standardAnswer, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 多选题判分：将选项集合规范化后比较（忽略顺序与大小写）。
    /// </summary>
    /// <param name="userAnswer">用户答案（逗号分隔）。</param>
    /// <param name="standardAnswer">标准答案（逗号分隔）。</param>
    /// <returns>是否正确。</returns>
    private static bool GradeMultipleChoice(string userAnswer, string standardAnswer)
    {
        var u = SplitOptions(userAnswer);
        var s = SplitOptions(standardAnswer);
        if (u.Count != s.Count)
        {
            return false;
        }

        u.Sort(StringComparer.OrdinalIgnoreCase);
        s.Sort(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < u.Count; i++)
        {
            if (!string.Equals(u[i], s[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断题判分：兼容“对/错”“true/false”“yes/no”等写法。
    /// </summary>
    /// <param name="userAnswer">用户答案。</param>
    /// <param name="standardAnswer">标准答案。</param>
    /// <returns>是否正确。</returns>
    private static bool GradeTrueFalse(string userAnswer, string standardAnswer)
    {
        var u = NormalizeTrueFalse(userAnswer);
        var s = NormalizeTrueFalse(standardAnswer);
        return string.Equals(u, s, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 简答题判分：演示版采用“关键词命中”策略（标准答案用分号/逗号分隔多个关键词）。
    /// </summary>
    /// <param name="userAnswer">用户答案。</param>
    /// <param name="standardAnswer">标准答案（可包含多个关键词）。</param>
    /// <returns>是否正确。</returns>
    private static bool GradeShortAnswer(string userAnswer, string standardAnswer)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            return false;
        }

        var keys = standardAnswer
            .Split(new[] { ';', '；', ',', '，', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .ToList();

        if (keys.Count == 0)
        {
            return string.Equals(userAnswer, standardAnswer, StringComparison.OrdinalIgnoreCase);
        }

        // 所有关键词都需要出现在用户答案中（不区分大小写）。
        return keys.All(k => userAnswer.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    /// <summary>
    /// 将判断题答案映射到统一的“对/错”表达。
    /// </summary>
    /// <param name="text">用户或标准答案文本。</param>
    /// <returns>规范化文本。</returns>
    private static string NormalizeTrueFalse(string text)
    {
        var t = text.Trim();
        if (t.Equals("T", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("True", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("对", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("是", StringComparison.OrdinalIgnoreCase))
        {
            return "对";
        }

        if (t.Equals("F", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("False", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("N", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("No", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("错", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("否", StringComparison.OrdinalIgnoreCase))
        {
            return "错";
        }

        return t;
    }

    /// <summary>
    /// 拆分多选题选项键。
    /// </summary>
    /// <param name="text">逗号分隔文本。</param>
    /// <returns>选项列表。</returns>
    private static List<string> SplitOptions(string text)
    {
        return text
            .Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();
    }
}
