using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Infrastructure;

/// <summary>
/// 为演示种子题提供「细粒度知识点」短语池：与 <see cref="Question.Domain"/> 对应，不重复领域枚举信息。
/// </summary>
public static class SeedKnowledgePointCatalog
{
    /// <summary>
    /// 生成主知识点（用于 <see cref="Question.PrimaryKnowledgePoint"/>）及逗号分隔的 <see cref="Question.KnowledgeTags"/>。
    /// </summary>
    /// <param name="domain">题目领域。</param>
    /// <param name="indexInDomain">领域内题号（1..N）。</param>
    public static (string Primary, string KnowledgeTagsCsv) BuildForSeed(QuestionDomain domain, int indexInDomain)
    {
        var pool = Pool(domain);
        var i = (indexInDomain - 1) % pool.Length;
        var j = (indexInDomain + 2) % pool.Length;
        var a = pool[i];
        var b = pool[j];
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
        {
            b = pool[(i + 1) % pool.Length];
        }

        return (a, $"{a},{b}");
    }

    /// <summary>
    /// 返回某领域下的全部细知识点短语池（只读），供补全逻辑挑选与主知识点不同的次标签。
    /// </summary>
    public static IReadOnlyList<string> GetPool(QuestionDomain domain) => Pool(domain);

    /// <summary>
    /// 在领域池中选取与 <paramref name="excludePrimary"/> 不同的另一短语，用于拼出双标签的 <c>KnowledgeTags</c>。
    /// </summary>
    public static string PickSecondaryDistinct(QuestionDomain domain, string excludePrimary, long idSalt)
    {
        var pool = Pool(domain);
        if (pool.Length == 0)
        {
            return excludePrimary;
        }

        for (var o = 0; o < pool.Length; o++)
        {
            var idx = (int)((Math.Abs(idSalt) + o * 31L) % pool.Length);
            var c = pool[idx];
            if (!c.Equals(excludePrimary, StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }

        return pool[0];
    }

    /// <summary>
    /// 领域级兜底主知识点：多题可共用同名，表示该领域综合巩固（非题干关键词命中时的最后手段）。
    /// </summary>
    public static string DomainCoarseFallbackPrimary(QuestionDomain domain) => domain switch
    {
        QuestionDomain.Uncategorized => "通识·综合掌握",
        QuestionDomain.Python => "Python·综合掌握",
        QuestionDomain.C => "C语言·综合掌握",
        QuestionDomain.CPlusPlus => "C++·综合掌握",
        QuestionDomain.CSharp => "C#·综合掌握",
        QuestionDomain.Rust => "Rust·综合掌握",
        QuestionDomain.Java => "Java·综合掌握",
        QuestionDomain.JavaScript => "JavaScript·综合掌握",
        QuestionDomain.Go => "Go·综合掌握",
        QuestionDomain.DataStructure => "数据结构·综合掌握",
        QuestionDomain.Database => "数据库·综合掌握",
        QuestionDomain.OperatingSystem => "操作系统·综合掌握",
        QuestionDomain.ComputerNetwork => "计算机网络·综合掌握",
        _ => "综合掌握"
    };

    private static string[] Pool(QuestionDomain domain) => domain switch
    {
        QuestionDomain.Uncategorized => new[]
        {
            "需求变更管理", "版本控制基础", "代码评审要点", "文档与注释", "缺陷优先级", "迭代计划", "风险识别", "可维护性"
        },
        QuestionDomain.Python => new[]
        {
            "列表与元组", "字典与集合", "推导式", "迭代器与生成器", "异常处理", "模块与包", "装饰器", "上下文管理器"
        },
        QuestionDomain.C => new[]
        {
            "指针与地址", "数组与字符串", "结构体与联合体", "预处理器", "存储期与链接", "函数指针", "动态内存", "文件IO"
        },
        QuestionDomain.CPlusPlus => new[]
        {
            "RAII与析构", "拷贝与移动语义", "虚函数与多态", "模板基础", "STL容器", "智能指针", "运算符重载", "异常安全"
        },
        QuestionDomain.CSharp => new[]
        {
            "值类型与引用类型", "LINQ基础", "async与await", "委托与事件", "接口与抽象类", "装箱拆箱", "Span与内存", "可空引用"
        },
        QuestionDomain.Rust => new[]
        {
            "所有权与移动", "借用与生命周期", "Trait与泛型", "模式匹配", "错误处理Result", "模块系统", "迭代器", "并发Send"
        },
        QuestionDomain.Java => new[]
        {
            "JVM内存模型", "泛型与擦除", "集合框架", "并发与锁", "异常体系", "接口与抽象类", "反射基础", "Stream API"
        },
        QuestionDomain.JavaScript => new[]
        {
            "作用域与闭包", "原型与继承", "异步Promise", "事件循环", "this绑定", "模块化ESM", "类型强制转换", "数组方法"
        },
        QuestionDomain.Go => new[]
        {
            "goroutine调度", "channel通信", "接口隐式实现", "defer语义", "错误处理惯用法", "切片与映射", "包与可见性", "竞态与锁"
        },
        QuestionDomain.DataStructure => new[]
        {
            "时间复杂度", "链表操作", "栈的应用", "队列与双端队列", "二叉树遍历", "堆与优先队列", "哈希冲突处理", "图遍历"
        },
        QuestionDomain.Database => new[]
        {
            "范式与冗余", "主键与外键", "索引选择", "事务ACID", "隔离级别", "锁与死锁", "JOIN类型", "查询优化思路"
        },
        QuestionDomain.OperatingSystem => new[]
        {
            "进程状态切换", "线程与内核线程", "调度算法", "分页与分段", "虚拟内存", "文件描述符", "死锁条件", "系统调用"
        },
        QuestionDomain.ComputerNetwork => new[]
        {
            "OSI与TCP/IP", "三次握手", "流量与拥塞控制", "DNS解析", "HTTP状态码", "子网划分", "NAT概念", "UDP适用场景"
        },
        _ => new[] { "通用考点A", "通用考点B", "通用考点C", "通用考点D", "通用考点E", "通用考点F", "通用考点G", "通用考点H" }
    };
}
