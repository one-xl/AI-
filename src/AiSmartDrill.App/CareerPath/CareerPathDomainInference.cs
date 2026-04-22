using AiSmartDrill.App.Domain;
using AiSmartDrill.App.Drill.Ai;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 根据 CareerPath 技能包中的知识点短语与岗位摘要，推断最接近的题库领域，并生成 AI 出题所需的标签提示。
/// </summary>
public static class CareerPathDomainInference
{
    private sealed record Rule(QuestionDomain Domain, string Keyword, int Weight);

    private static readonly Rule[] Rules =
    {
        new(QuestionDomain.Python, "python", 8),
        new(QuestionDomain.Python, "django", 7),
        new(QuestionDomain.Python, "flask", 7),
        new(QuestionDomain.Python, "pandas", 7),
        new(QuestionDomain.Python, "numpy", 7),
        new(QuestionDomain.Python, "pytest", 6),
        new(QuestionDomain.Python, "asyncio", 6),

        new(QuestionDomain.C, "c语言", 8),
        new(QuestionDomain.C, "ansi c", 7),
        new(QuestionDomain.C, "指针", 4),
        new(QuestionDomain.C, "malloc", 5),
        new(QuestionDomain.C, "free", 5),

        new(QuestionDomain.CPlusPlus, "c++", 8),
        new(QuestionDomain.CPlusPlus, "stl", 6),
        new(QuestionDomain.CPlusPlus, "template", 6),
        new(QuestionDomain.CPlusPlus, "模板", 5),
        new(QuestionDomain.CPlusPlus, "智能指针", 6),

        new(QuestionDomain.CSharp, "c#", 8),
        new(QuestionDomain.CSharp, ".net", 8),
        new(QuestionDomain.CSharp, "dotnet", 8),
        new(QuestionDomain.CSharp, "asp.net", 7),
        new(QuestionDomain.CSharp, "entity framework", 7),
        new(QuestionDomain.CSharp, "ef core", 7),
        new(QuestionDomain.CSharp, "wpf", 6),
        new(QuestionDomain.CSharp, "linq", 6),
        new(QuestionDomain.CSharp, "clr", 6),

        new(QuestionDomain.Rust, "rust", 8),
        new(QuestionDomain.Rust, "cargo", 7),
        new(QuestionDomain.Rust, "所有权", 7),
        new(QuestionDomain.Rust, "借用", 7),
        new(QuestionDomain.Rust, "生命周期", 7),

        new(QuestionDomain.Java, "java", 8),
        new(QuestionDomain.Java, "jvm", 7),
        new(QuestionDomain.Java, "spring", 7),
        new(QuestionDomain.Java, "mybatis", 6),
        new(QuestionDomain.Java, "maven", 6),
        new(QuestionDomain.Java, "jdk", 7),

        new(QuestionDomain.JavaScript, "javascript", 8),
        new(QuestionDomain.JavaScript, "typescript", 8),
        new(QuestionDomain.JavaScript, "node", 7),
        new(QuestionDomain.JavaScript, "nodejs", 7),
        new(QuestionDomain.JavaScript, "react", 7),
        new(QuestionDomain.JavaScript, "vue", 7),
        new(QuestionDomain.JavaScript, "webpack", 6),
        new(QuestionDomain.JavaScript, "promise", 5),

        new(QuestionDomain.Go, "golang", 8),
        new(QuestionDomain.Go, "go语言", 8),
        new(QuestionDomain.Go, "gin", 6),
        new(QuestionDomain.Go, "gorm", 6),
        new(QuestionDomain.Go, "goroutine", 7),
        new(QuestionDomain.Go, "channel", 7),

        new(QuestionDomain.DataStructure, "数据结构", 8),
        new(QuestionDomain.DataStructure, "算法", 7),
        new(QuestionDomain.DataStructure, "二叉树", 7),
        new(QuestionDomain.DataStructure, "链表", 6),
        new(QuestionDomain.DataStructure, "栈", 5),
        new(QuestionDomain.DataStructure, "队列", 5),
        new(QuestionDomain.DataStructure, "动态规划", 7),
        new(QuestionDomain.DataStructure, "bfs", 6),
        new(QuestionDomain.DataStructure, "dfs", 6),

        new(QuestionDomain.Database, "数据库", 8),
        new(QuestionDomain.Database, "sql", 7),
        new(QuestionDomain.Database, "mysql", 7),
        new(QuestionDomain.Database, "postgresql", 7),
        new(QuestionDomain.Database, "oracle", 7),
        new(QuestionDomain.Database, "redis", 6),
        new(QuestionDomain.Database, "事务", 6),
        new(QuestionDomain.Database, "索引", 6),
        new(QuestionDomain.Database, "join", 5),

        new(QuestionDomain.OperatingSystem, "操作系统", 8),
        new(QuestionDomain.OperatingSystem, "进程", 6),
        new(QuestionDomain.OperatingSystem, "线程", 5),
        new(QuestionDomain.OperatingSystem, "死锁", 7),
        new(QuestionDomain.OperatingSystem, "虚拟内存", 7),
        new(QuestionDomain.OperatingSystem, "页表", 7),
        new(QuestionDomain.OperatingSystem, "调度", 6),

        new(QuestionDomain.ComputerNetwork, "计算机网络", 8),
        new(QuestionDomain.ComputerNetwork, "tcp", 7),
        new(QuestionDomain.ComputerNetwork, "udp", 7),
        new(QuestionDomain.ComputerNetwork, "http", 7),
        new(QuestionDomain.ComputerNetwork, "https", 7),
        new(QuestionDomain.ComputerNetwork, "dns", 7),
        new(QuestionDomain.ComputerNetwork, "socket", 6),
        new(QuestionDomain.ComputerNetwork, "cdn", 5),

        new(QuestionDomain.Uncategorized, "git", 5),
        new(QuestionDomain.Uncategorized, "代码评审", 6),
        new(QuestionDomain.Uncategorized, "需求分析", 6),
        new(QuestionDomain.Uncategorized, "测试用例", 5),
        new(QuestionDomain.Uncategorized, "敏捷", 5)
    };

    /// <summary>
    /// 推断技能包知识点最接近的题库领域；无法判断时回退为 <see cref="QuestionDomain.Uncategorized"/>。
    /// </summary>
    public static QuestionDomain InferDomain(IReadOnlyList<string> skills, string? jobSummary = null)
    {
        var haystacks = BuildHaystacks(skills, jobSummary);
        var scores = new Dictionary<QuestionDomain, int>();
        foreach (var hay in haystacks)
        {
            foreach (var rule in Rules)
            {
                if (hay.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase))
                {
                    scores[rule.Domain] = scores.TryGetValue(rule.Domain, out var score)
                        ? score + rule.Weight
                        : rule.Weight;
                }
            }
        }

        if (scores.Count == 0)
        {
            return QuestionDomain.Uncategorized;
        }

        return scores
            .OrderByDescending(static x => x.Value)
            .ThenBy(static x => x.Key)
            .First()
            .Key;
    }

    /// <summary>
    /// 归一化技能短语，用于生成 KnowledgeTags / TopicKeywords 提示。
    /// </summary>
    public static IReadOnlyList<string> NormalizeSkillHints(IReadOnlyList<string> skills, int maxCount = 10)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in skills)
        {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            set.Add(trimmed);
            foreach (var token in RecommendationMatcher.Tokenize(trimmed))
            {
                if (token.Length >= 2)
                {
                    set.Add(token);
                }
            }
        }

        return set
            .OrderByDescending(static x => x.Length)
            .ThenBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxCount))
            .ToList();
    }

    /// <summary>
    /// 拼出供模型使用的知识点提示文案。
    /// </summary>
    public static string BuildKnowledgeTagsHint(IReadOnlyList<string> skills, int maxCount = 10) =>
        string.Join("、", NormalizeSkillHints(skills, maxCount));

    /// <summary>
    /// 拼出供模型使用的分类标签提示文案。
    /// </summary>
    public static string BuildTopicTagsHint(QuestionDomain domain, IReadOnlyList<string> skills)
    {
        var hints = NormalizeSkillHints(skills, 6);
        if (hints.Count == 0)
        {
            return MapDomainDisplay(domain);
        }

        return $"{MapDomainDisplay(domain)}；{string.Join("、", hints)}";
    }

    /// <summary>
    /// 将枚举领域映射为界面文案。
    /// </summary>
    public static string MapDomainDisplay(QuestionDomain domain) => domain switch
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

    private static IReadOnlyList<string> BuildHaystacks(IReadOnlyList<string> skills, string? jobSummary)
    {
        var list = new List<string>();
        foreach (var raw in skills)
        {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            list.Add(trimmed);
            foreach (var token in RecommendationMatcher.Tokenize(trimmed))
            {
                if (token.Length >= 2)
                {
                    list.Add(token);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(jobSummary))
        {
            list.Add(jobSummary.Trim());
        }

        return list
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
