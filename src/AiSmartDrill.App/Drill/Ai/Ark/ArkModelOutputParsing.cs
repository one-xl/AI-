namespace AiSmartDrill.App.Drill.Ai.Ark;

/// <summary>
/// 从豆包/方舟返回的正文中提取可反序列化的 JSON 片段（去除 Markdown 代码围栏与前后说明）。
/// </summary>
public static class ArkModelOutputParsing
{
    /// <summary>
    /// 去除常见 <c>```json</c> 围栏并裁剪空白，便于 JSON 反序列化。
    /// </summary>
    public static string StripMarkdownFence(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var s = raw.Trim();
        if (!s.StartsWith("```", StringComparison.Ordinal))
            return s;

        var firstLineBreak = s.IndexOf('\n');
        if (firstLineBreak < 0)
            return s;

        s = s[(firstLineBreak + 1)..];
        var fenceEnd = s.LastIndexOf("```", StringComparison.Ordinal);
        if (fenceEnd >= 0)
            s = s[..fenceEnd];

        return s.Trim();
    }

    /// <summary>
    /// 尝试从模型输出中截取第一个完整的 JSON 对象或数组（按大括号/方括号配对），用于模型在 JSON 前后加了说明文字的情况。
    /// </summary>
    public static string ExtractFirstJsonValue(string text)
    {
        var s = StripMarkdownFence(text);
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        var objStart = s.IndexOf('{');
        var arrStart = s.IndexOf('[');
        int start;
        char open, close;
        if (arrStart >= 0 && (objStart < 0 || arrStart < objStart))
        {
            start = arrStart;
            open = '[';
            close = ']';
        }
        else if (objStart >= 0)
        {
            start = objStart;
            open = '{';
            close = '}';
        }
        else
            return s;

        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = start; i < s.Length; i++)
        {
            var c = s[i];
            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (c == open)
                depth++;
            else if (c == close)
            {
                depth--;
                if (depth == 0)
                    return s.Substring(start, i - start + 1);
            }
        }

        return s;
    }
}
