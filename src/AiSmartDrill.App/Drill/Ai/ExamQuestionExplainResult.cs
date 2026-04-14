namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 考试中「问 AI」的返回：先结论后详解，便于学习者先看到要点再读展开。
/// </summary>
/// <param name="Conclusion">最简结论（得分要点、选项键或对错）。</param>
/// <param name="Detail">具体解析正文。</param>
public sealed record ExamQuestionExplainResult(string Conclusion, string Detail)
{
    /// <summary>
    /// 将模型原文按【结论】/【详解】标记拆分；若无标记则尽量取首行作结论。
    /// </summary>
    public static ExamQuestionExplainResult ParseFromModelOutput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ExamQuestionExplainResult(string.Empty, string.Empty);
        }

        var t = text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        const string cTag = "【结论】";
        const string dTag = "【详解】";
        var ci = t.IndexOf(cTag, StringComparison.Ordinal);
        var di = t.IndexOf(dTag, StringComparison.Ordinal);
        if (ci >= 0 && di > ci)
        {
            var conclusion = t.Substring(ci + cTag.Length, di - ci - cTag.Length).Trim('\n', ' ', '\t');
            var detail = t[(di + dTag.Length)..].Trim('\n', ' ', '\t');
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = "（模型未写详解。）";
            }

            return new ExamQuestionExplainResult(ToDisplayNewlines(conclusion), ToDisplayNewlines(detail));
        }

        if (di >= 0)
        {
            var headline = di > 0 ? t[..di].Trim('\n', ' ', '\t') : string.Empty;
            var detail = t[(di + dTag.Length)..].Trim('\n', ' ', '\t');
            return new ExamQuestionExplainResult(ToDisplayNewlines(headline), ToDisplayNewlines(detail));
        }

        var nl = t.IndexOf('\n', StringComparison.Ordinal);
        if (nl > 0 && nl < t.Length - 1)
        {
            var first = t[..nl].Trim();
            var rest = t[(nl + 1)..].Trim();
            return new ExamQuestionExplainResult(ToDisplayNewlines(first), ToDisplayNewlines(rest));
        }

        return new ExamQuestionExplainResult(string.Empty, ToDisplayNewlines(t));
    }

    private static string ToDisplayNewlines(string s) => s.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
}
