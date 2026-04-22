using System.Text.Json;
using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Infrastructure;

/// <summary>
/// 构建各领域的批量演示题目：每个 <see cref="QuestionDomain"/> 固定 20 道，覆盖单选/多选/判断/简答/填空，便于联调与压力测试。
/// </summary>
public static class QuestionSeedBuilder
{
    /// <summary>
    /// 每个领域生成的题目数量。
    /// </summary>
    public const int QuestionsPerDomain = 20;

    /// <summary>
    /// 生成全部种子题目（13 个领域 × 20 = 260 道）。
    /// </summary>
    /// <param name="nowUtc">写入 <see cref="Question.CreatedAtUtc"/> 的时间（UTC）。</param>
    /// <returns>题目列表（按领域枚举顺序、题号升序）。</returns>
    public static List<Question> BuildAllSeedQuestions(DateTime nowUtc)
    {
        string Opt(params string[] lines) => JsonSerializer.Serialize(lines);
        var list = new List<Question>(13 * QuestionsPerDomain);

        foreach (QuestionDomain domain in Enum.GetValues<QuestionDomain>())
        {
            var tag = DomainToKnowledgeTag(domain);
            for (var i = 1; i <= QuestionsPerDomain; i++)
            {
                var difficulty = (DifficultyLevel)((i + (int)domain) % 3);
                var kind = (i - 1) % 5;

                var (kpPrimary, kpCsv) = SeedKnowledgePointCatalog.BuildForSeed(domain, i);
                switch (kind)
                {
                    case 0:
                    {
                        var variant = QuestionSeedContent.Variant(i);
                        var (correctLetter, optionLines) = QuestionSeedContent.SingleChoice(domain, variant);
                        var (topicTags, topicKeywords) = BuildTopicMeta(i, tag, "单选");

                        list.Add(new Question
                        {
                            Type = QuestionType.SingleChoice,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = $"【{tag}】第{i}题（单选）：{SingleChoiceStem(domain, i)}",
                            StandardAnswer = correctLetter.ToString(),
                            OptionsJson = Opt(optionLines),
                            KnowledgeTags = kpCsv,
                            PrimaryKnowledgePoint = kpPrimary,
                            TopicTags = topicTags,
                            TopicKeywords = topicKeywords,
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    }
                    case 1:
                    {
                        var tfRound = (i - 2) / 4;
                        var answerTrue = tfRound % 2 == 0;
                        var (topicTags, topicKeywords) = BuildTopicMeta(i, tag, "判断");
                        list.Add(new Question
                        {
                            Type = QuestionType.TrueFalse,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = $"【{tag}】第{i}题（判断）：{TrueFalseStem(domain, i, answerTrue)}",
                            StandardAnswer = answerTrue ? "对" : "错",
                            OptionsJson = null,
                            KnowledgeTags = kpCsv,
                            PrimaryKnowledgePoint = kpPrimary,
                            TopicTags = topicTags,
                            TopicKeywords = topicKeywords,
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    }
                    case 2:
                    {
                        var variantMc = QuestionSeedContent.Variant(i);
                        var (multiStandard, multiOpts) = QuestionSeedContent.MultipleChoice(domain, variantMc);
                        var (topicTags, topicKeywords) = BuildTopicMeta(i, tag, "多选");
                        list.Add(new Question
                        {
                            Type = QuestionType.MultipleChoice,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = $"【{tag}】第{i}题（多选）：{MultipleChoiceStem(domain, i)}",
                            StandardAnswer = multiStandard,
                            OptionsJson = Opt(multiOpts),
                            KnowledgeTags = kpCsv,
                            PrimaryKnowledgePoint = kpPrimary,
                            TopicTags = topicTags,
                            TopicKeywords = topicKeywords,
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    }
                    case 3:
                    {
                        var variantSa = QuestionSeedContent.Variant(i);
                        var (shortStem, shortKeys) = QuestionSeedContent.ShortAnswer(domain, variantSa, i);
                        var (topicTags, topicKeywords) = BuildTopicMeta(i, tag, "简答");
                        list.Add(new Question
                        {
                            Type = QuestionType.ShortAnswer,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = shortStem,
                            StandardAnswer = shortKeys,
                            OptionsJson = null,
                            KnowledgeTags = kpCsv,
                            PrimaryKnowledgePoint = kpPrimary,
                            TopicTags = topicTags,
                            TopicKeywords = topicKeywords,
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    }
                    default:
                    {
                        var variantFb = QuestionSeedContent.Variant(i);
                        var (fillStem, fillRegex) = QuestionSeedContent.FillBlank(domain, variantFb, i);
                        var (topicTags, topicKeywords) = BuildTopicMeta(i, tag, "填空");
                        list.Add(new Question
                        {
                            Type = QuestionType.FillInBlank,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = fillStem,
                            StandardAnswer = fillRegex,
                            OptionsJson = null,
                            KnowledgeTags = kpCsv,
                            PrimaryKnowledgePoint = kpPrimary,
                            TopicTags = topicTags,
                            TopicKeywords = topicKeywords,
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    }
                }
            }
        }

        return list;
    }

    /// <summary>
    /// 为种子题生成分类标签与检索关键词，便于 AI 推荐按领域+标签筛选。
    /// </summary>
    private static (string TopicTags, string TopicKeywords) BuildTopicMeta(
        int indexInDomain,
        string domainTag,
        string typeLabel)
    {
        var tier = (indexInDomain % 4) switch
        {
            0 => "基础",
            1 => "进阶",
            2 => "综合",
            _ => "易错"
        };
        var track = (indexInDomain % 3) switch
        {
            0 => "概念",
            1 => "应用",
            _ => "辨析"
        };
        var extras = TopicTagCatalog.SeedExtraTags;
        var extraA = extras[(indexInDomain * 3) % extras.Count];
        var extraB = extras[(indexInDomain * 7 + 5) % extras.Count];
        var topicTags = $"{domainTag}/{tier},{domainTag}/{track},{typeLabel},{extraA},{extraB}";
        var topicKeywords = $"{tier}；{track}；{domainTag}；{typeLabel}；{extraA}；{extraB}；题组{indexInDomain % 9}";
        return (topicTags, topicKeywords);
    }

    /// <summary>
    /// 将领域映射为知识点标签（与 UI 领域名称一致或接近）。
    /// </summary>
    private static string DomainToKnowledgeTag(QuestionDomain domain) => domain switch
    {
        QuestionDomain.Uncategorized => "通识",
        QuestionDomain.Python => "Python",
        QuestionDomain.C => "C语言",
        QuestionDomain.CPlusPlus => "C++",
        QuestionDomain.CSharp => "C#",
        QuestionDomain.Rust => "Rust",
        QuestionDomain.Java => "Java",
        QuestionDomain.JavaScript => "JavaScript",
        QuestionDomain.Go => "Go",
        QuestionDomain.DataStructure => "数据结构",
        QuestionDomain.Database => "数据库",
        QuestionDomain.OperatingSystem => "操作系统",
        QuestionDomain.ComputerNetwork => "计算机网络",
        _ => "通识"
    };

    private static string SingleChoiceStem(QuestionDomain domain, int index) => domain switch
    {
        QuestionDomain.Python =>
            $"关于 Python 基础（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.C =>
            $"关于 C 语言语法与语义（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.CPlusPlus =>
            $"关于 C++ 面向对象与内存（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.CSharp =>
            $"关于 C# 与 .NET（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.Rust =>
            $"关于 Rust 所有权与借用（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.Java =>
            $"关于 JVM 与 Java 基础（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.JavaScript =>
            $"关于 JavaScript 语言特性（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.Go =>
            $"关于 Go 语言并发与接口（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.DataStructure =>
            $"关于数据结构（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.Database =>
            $"关于关系数据库与 SQL（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.OperatingSystem =>
            $"关于操作系统概念（题组 {index}），从下列选项中选出最恰当的一项。",
        QuestionDomain.ComputerNetwork =>
            $"关于计算机网络（题组 {index}），从下列选项中选出最恰当的一项。",
        _ =>
            $"通用软件工程基础（题组 {index}），从下列选项中选出最恰当的一项。"
    };

    /// <summary>
    /// 判断题陈述：<paramref name="statementIsTrue"/> 与标准答案「对/错」一致。
    /// </summary>
    private static string TrueFalseStem(QuestionDomain domain, int index, bool statementIsTrue) => domain switch
    {
        QuestionDomain.Python => statementIsTrue
            ? $"Python 中 list 是可变序列（题组 {index}）。"
            : $"Python 中 tuple 与 list 一样是可变序列（题组 {index}）。",
        QuestionDomain.C => statementIsTrue
            ? $"C 语言函数可以递归调用（题组 {index}）。"
            : $"C 语言源文件扩展名必须是 .cpp（题组 {index}）。",
        QuestionDomain.CPlusPlus => statementIsTrue
            ? $"C++ 中 class 的默认成员访问为 private（题组 {index}）。"
            : $"C++ 中 struct 的默认成员访问为 private（题组 {index}）。",
        QuestionDomain.CSharp => statementIsTrue
            ? $"C# 中 string 类型为引用类型（题组 {index}）。"
            : $"C# 中 int 类型为引用类型（题组 {index}）。",
        QuestionDomain.Rust => statementIsTrue
            ? $"Rust 在编译期进行借用检查（题组 {index}）。"
            : $"Rust 完全不在编译期检查内存安全（题组 {index}）。",
        QuestionDomain.Java => statementIsTrue
            ? $"Java 源码通常编译为字节码在 JVM 上运行（题组 {index}）。"
            : $"Java 源码直接编译为本机机器码且无虚拟机（题组 {index}）。",
        QuestionDomain.JavaScript => statementIsTrue
            ? $"JavaScript 可在浏览器中执行（题组 {index}）。"
            : $"JavaScript 只能在服务器执行而不能在浏览器执行（题组 {index}）。",
        QuestionDomain.Go => statementIsTrue
            ? $"Go 使用 goroutine 表达并发（题组 {index}）。"
            : $"Go 不支持并发编程（题组 {index}）。",
        QuestionDomain.DataStructure => statementIsTrue
            ? $"栈是一种后进先出（LIFO）结构（题组 {index}）。"
            : $"队列是一种后进先出结构（题组 {index}）。",
        QuestionDomain.Database => statementIsTrue
            ? $"关系数据库事务可提供原子性（题组 {index}）。"
            : $"SQL 事务必然破坏数据一致性（题组 {index}）。",
        QuestionDomain.OperatingSystem => statementIsTrue
            ? $"进程是资源分配的基本单位之一（题组 {index}）。"
            : $"操作系统从不进行进程调度（题组 {index}）。",
        QuestionDomain.ComputerNetwork => statementIsTrue
            ? $"TCP 提供面向连接的可靠传输（题组 {index}）。"
            : $"UDP 与 TCP 都必须建立连接后才能传数据（题组 {index}）。",
        _ => statementIsTrue
            ? $"迭代开发可以分阶段交付软件（题组 {index}）。"
            : $"软件需求在项目中从不变化（题组 {index}）。"
    };

    private static string MultipleChoiceStem(QuestionDomain domain, int index) => domain switch
    {
        QuestionDomain.Python => $"下列关于 Python 的描述中正确的有（题组 {index}）。",
        QuestionDomain.C => $"下列关于 C 语言的描述中正确的有（题组 {index}）。",
        QuestionDomain.CPlusPlus => $"下列关于 C++ 的描述中正确的有（题组 {index}）。",
        QuestionDomain.CSharp => $"下列关于 C# 的描述中正确的有（题组 {index}）。",
        QuestionDomain.Rust => $"下列关于 Rust 的描述中正确的有（题组 {index}）。",
        QuestionDomain.Java => $"下列关于 Java 的描述中正确的有（题组 {index}）。",
        QuestionDomain.JavaScript => $"下列关于 JavaScript 的描述中正确的有（题组 {index}）。",
        QuestionDomain.Go => $"下列关于 Go 的描述中正确的有（题组 {index}）。",
        QuestionDomain.DataStructure => $"下列关于数据结构的描述中正确的有（题组 {index}）。",
        QuestionDomain.Database => $"下列关于数据库的描述中正确的有（题组 {index}）。",
        QuestionDomain.OperatingSystem => $"下列关于操作系统的描述中正确的有（题组 {index}）。",
        QuestionDomain.ComputerNetwork => $"下列关于计算机网络的描述中正确的有（题组 {index}）。",
        _ => $"下列关于软件工程的描述中正确的有（题组 {index}）。"
    };
}
