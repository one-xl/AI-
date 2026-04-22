namespace AiSmartDrill.App.Domain;

/// <summary>
/// 统计弱项知识点、推断主知识点时忽略的噪声词（题型、种子标记等），避免与细粒度考点混淆。
/// </summary>
public static class KnowledgeTagStopwords
{
    private static readonly HashSet<string> Tokens = new(StringComparer.OrdinalIgnoreCase)
    {
        string.Empty,
        "批量种子",
        "未分类",
        "单选",
        "多选",
        "判断",
        "简答",
        "填空",
        "通识",
        "Python",
        "C语言",
        "C++",
        "C#",
        "Rust",
        "Java",
        "JavaScript",
        "Go",
        "数据结构",
        "数据库",
        "操作系统",
        "计算机网络"
    };

    /// <summary>
    /// 判断给定分词是否应参与「细知识点」弱项统计（为 true 表示忽略）。
    /// </summary>
    public static bool IsStopword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        return Tokens.Contains(token.Trim());
    }
}
