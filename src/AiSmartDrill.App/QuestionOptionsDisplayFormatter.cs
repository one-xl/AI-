using System.Text;
using System.Text.Json;

namespace AiSmartDrill.App;

/// <summary>
/// 将题库中的 <c>OptionsJson</c> 格式化为与考试页一致的 A/B/C… 多行展示，供表格与错题本复用。
/// </summary>
public static class QuestionOptionsDisplayFormatter
{
    /// <summary>
    /// 去掉选项正文中与当前键重复的「字母 + 句点/顿号」前缀（支持嵌套如「A. A. 正文」），
    /// 以便 JSON 里已带「A. xxx」时不再与外层 <c>A. </c> 拼接成双前缀。
    /// </summary>
    /// <param name="optionKey">当前行键（单字符 A–Z）。</param>
    /// <param name="storedLine">JSON 数组中的原始字符串。</param>
    /// <returns>去重后的正文（不含行首键）。</returns>
    public static string StripRedundantLeadingOptionPrefix(string optionKey, string storedLine)
    {
        if (string.IsNullOrWhiteSpace(storedLine) || optionKey.Length != 1)
        {
            return storedLine.Trim();
        }

        var keyUpper = char.ToUpperInvariant(optionKey[0]);
        if (keyUpper is < 'A' or > 'Z')
        {
            return storedLine.Trim();
        }

        var t = storedLine.Trim();
        while (t.Length >= 2
               && char.ToUpperInvariant(t[0]) == keyUpper
               && (t[1] == '.' || t[1] == '．' || t[1] == '、'))
        {
            t = t[2..].TrimStart();
        }

        return t;
    }

    /// <summary>
    /// 将选项 JSON 解析为人类可读文本（每行一项）。
    /// </summary>
    public static string FormatForDisplay(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return "（无选项：非客观题或未录入 OptionsJson）";
        }

        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(optionsJson);
            if (arr is null || arr.Count == 0)
            {
                return optionsJson;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < arr.Count; i++)
            {
                var label = ((char)('A' + i)).ToString();
                var body = StripRedundantLeadingOptionPrefix(label, arr[i]);
                var line = string.IsNullOrEmpty(body) ? $"{label}." : $"{label}. {body}";
                sb.AppendLine(line);
            }

            return sb.ToString().TrimEnd();
        }
        catch
        {
            return optionsJson;
        }
    }
}
