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

                switch (kind)
                {
                    case 0:
                    {
                        var correctLetter = (char)('A' + ((i - 1) % 4));
                        var opts = new string[4];
                        for (var k = 0; k < 4; k++)
                        {
                            var letter = (char)('A' + k);
                            opts[k] = letter == correctLetter
                                ? $"{letter}. 本题正确选项"
                                : $"{letter}. 干扰项（非正确）";
                        }

                        list.Add(new Question
                        {
                            Type = QuestionType.SingleChoice,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = $"【{tag}】第{i}题（单选）：{SingleChoiceStem(domain, i)}",
                            StandardAnswer = correctLetter.ToString(),
                            OptionsJson = Opt(opts),
                            KnowledgeTags = $"{tag},单选,批量种子",
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    }
                    case 1:
                    {
                        var tfRound = (i - 2) / 4;
                        var answerTrue = tfRound % 2 == 0;
                        list.Add(new Question
                        {
                            Type = QuestionType.TrueFalse,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = $"【{tag}】第{i}题（判断）：{TrueFalseStem(domain, i, answerTrue)}",
                            StandardAnswer = answerTrue ? "对" : "错",
                            OptionsJson = null,
                            KnowledgeTags = $"{tag},判断,批量种子",
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    }
                    case 2:
                        list.Add(new Question
                        {
                            Type = QuestionType.MultipleChoice,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = $"【{tag}】第{i}题（多选）：{MultipleChoiceStem(domain, i)}",
                            StandardAnswer = "A,C",
                            OptionsJson = Opt(
                                "A. 正确表述之一",
                                "B. 错误表述",
                                "C. 正确表述之二",
                                "D. 错误表述"),
                            KnowledgeTags = $"{tag},多选,批量种子",
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    case 3:
                        list.Add(new Question
                        {
                            Type = QuestionType.ShortAnswer,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = $"【{tag}】第{i}题（简答）：{ShortAnswerStem(domain, i)}",
                            StandardAnswer = ShortAnswerKey(domain),
                            OptionsJson = null,
                            KnowledgeTags = $"{tag},简答,批量种子",
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                    default:
                        list.Add(new Question
                        {
                            Type = QuestionType.FillInBlank,
                            Difficulty = difficulty,
                            Domain = domain,
                            Stem = $"【{tag}】第{i}题（填空）：{FillBlankStem(domain, i)}",
                            StandardAnswer = "(关键|核心|重点)",
                            OptionsJson = null,
                            KnowledgeTags = $"{tag},填空,批量种子",
                            IsEnabled = true,
                            CreatedAtUtc = nowUtc
                        });
                        break;
                }
            }
        }

        return list;
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

    /// <summary>
    /// 填空题题干：提示用户输入需命中正则关键词。
    /// </summary>
    private static string FillBlankStem(QuestionDomain domain, int index) => domain switch
    {
        QuestionDomain.Python => $"请用一句话概括学习 Python 时的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.C => $"请用一句话概括 C 语言学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.CPlusPlus => $"请用一句话概括 C++ 学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.CSharp => $"请用一句话概括 C# 学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.Rust => $"请用一句话概括 Rust 学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.Java => $"请用一句话概括 Java 学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.JavaScript => $"请用一句话概括 JavaScript 学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.Go => $"请用一句话概括 Go 学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.DataStructure => $"请用一句话概括数据结构学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.Database => $"请用一句话概括数据库学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.OperatingSystem => $"请用一句话概括操作系统学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        QuestionDomain.ComputerNetwork => $"请用一句话概括计算机网络学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。",
        _ => $"请用一句话概括本领域学习的一个要点（题组 {index}），答案中须出现「关键」「核心」「重点」之一。"
    };

    private static string ShortAnswerStem(QuestionDomain domain, int index) => domain switch
    {
        QuestionDomain.Python => $"请写出与 Python 相关的任一标准库或工具名（题组 {index}）。",
        QuestionDomain.C => $"请写出 C 程序入口函数名（题组 {index}）。",
        QuestionDomain.CPlusPlus => $"请写出 C++ 中用于标准输入输出的头文件之一（题组 {index}）。",
        QuestionDomain.CSharp => $"请写出 C# 中用于异步的关键字之一（题组 {index}）。",
        QuestionDomain.Rust => $"请写出 Rust 包管理工具名（题组 {index}）。",
        QuestionDomain.Java => $"请写出 Java 源码文件扩展名（题组 {index}）。",
        QuestionDomain.JavaScript => $"请写出一种运行 JavaScript 的环境或引擎名（题组 {index}）。",
        QuestionDomain.Go => $"请写出 Go 模块文件常用名（题组 {index}）。",
        QuestionDomain.DataStructure => $"请写出一种线性结构名称（题组 {index}）。",
        QuestionDomain.Database => $"请写出一种关系型数据库产品名（题组 {index}）。",
        QuestionDomain.OperatingSystem => $"请写出进程调度相关概念之一（题组 {index}）。",
        QuestionDomain.ComputerNetwork => $"请写出 OSI 七层中任意一层名称（题组 {index}）。",
        _ => $"请写出软件开发流程中的任一环节名称（题组 {index}）。"
    };

    /// <summary>
    /// 简答题标准答案关键词（分号/逗号分隔多个关键词时判分需全部命中；此处每域仅给一个关键词，与对应题干一致，避免「答案过严」）。
    /// </summary>
    private static string ShortAnswerKey(QuestionDomain domain) => domain switch
    {
        QuestionDomain.Python => "pip",
        QuestionDomain.C => "main",
        QuestionDomain.CPlusPlus => "iostream",
        QuestionDomain.CSharp => "async",
        QuestionDomain.Rust => "cargo",
        QuestionDomain.Java => "java",
        QuestionDomain.JavaScript => "node",
        QuestionDomain.Go => "go.mod",
        QuestionDomain.DataStructure => "栈",
        QuestionDomain.Database => "SQL",
        QuestionDomain.OperatingSystem => "进程",
        QuestionDomain.ComputerNetwork => "TCP",
        _ => "需求"
    };
}
