using System.Collections.Generic;
using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Infrastructure;

/// <summary>
/// 根据题干/选项文本中的关键词，将题目映射到与领域强相关的细知识点短语（全部来自可核对的技术词汇，非大模型生成）。
/// </summary>
internal static class StemToKnowledgePointRouter
{
    private sealed record Rule(QuestionDomain Domain, string Needle, string Label);

    private static readonly Rule[] RulesOrderedByNeedleLength;

    static StemToKnowledgePointRouter()
    {
        var rules = new List<Rule>
        {
            // Python
            new(QuestionDomain.Python, "全局解释器锁", "迭代器与生成器"),
            new(QuestionDomain.Python, "virtualenv", "模块与包"),
            new(QuestionDomain.Python, "asyncio", "迭代器与生成器"),
            new(QuestionDomain.Python, "__init__", "模块与包"),
            new(QuestionDomain.Python, "venv", "模块与包"),
            new(QuestionDomain.Python, "import", "模块与包"),
            new(QuestionDomain.Python, "元组", "列表与元组"),
            new(QuestionDomain.Python, "tuple", "列表与元组"),
            new(QuestionDomain.Python, "列表", "列表与元组"),
            new(QuestionDomain.Python, "list", "列表与元组"),
            new(QuestionDomain.Python, "字典", "字典与集合"),
            new(QuestionDomain.Python, "dict", "字典与集合"),
            new(QuestionDomain.Python, "推导式", "推导式"),
            new(QuestionDomain.Python, "def ", "推导式"),
            new(QuestionDomain.Python, "装饰器", "装饰器"),
            new(QuestionDomain.Python, "with ", "上下文管理器"),
            new(QuestionDomain.Python, "异常", "异常处理"),
            new(QuestionDomain.Python, "GIL", "迭代器与生成器"),
            new(QuestionDomain.Python, "线程", "迭代器与生成器"),
            // C
            new(QuestionDomain.C, "malloc", "动态内存"),
            new(QuestionDomain.C, "free", "动态内存"),
            new(QuestionDomain.C, "realloc", "动态内存"),
            new(QuestionDomain.C, "指针", "指针与地址"),
            new(QuestionDomain.C, "static", "存储期与链接"),
            new(QuestionDomain.C, "#include", "预处理器"),
            new(QuestionDomain.C, "struct", "结构体与联合体"),
            new(QuestionDomain.C, "printf", "数组与字符串"),
            new(QuestionDomain.C, "头文件", "预处理器"),
            // C++
            new(QuestionDomain.CPlusPlus, "unique_ptr", "智能指针"),
            new(QuestionDomain.CPlusPlus, "shared_ptr", "智能指针"),
            new(QuestionDomain.CPlusPlus, "虚函数", "虚函数与多态"),
            new(QuestionDomain.CPlusPlus, "RAII", "RAII与析构"),
            new(QuestionDomain.CPlusPlus, "模板", "模板基础"),
            new(QuestionDomain.CPlusPlus, "析构", "RAII与析构"),
            new(QuestionDomain.CPlusPlus, "移动语义", "拷贝与移动语义"),
            new(QuestionDomain.CPlusPlus, "拷贝", "拷贝与移动语义"),
            // C#
            new(QuestionDomain.CSharp, "async", "async与await"),
            new(QuestionDomain.CSharp, "await", "async与await"),
            new(QuestionDomain.CSharp, "LINQ", "LINQ基础"),
            new(QuestionDomain.CSharp, "Task", "async与await"),
            new(QuestionDomain.CSharp, "接口", "接口与抽象类"),
            new(QuestionDomain.CSharp, "struct", "值类型与引用类型"),
            new(QuestionDomain.CSharp, "string", "值类型与引用类型"),
            new(QuestionDomain.CSharp, "int", "值类型与引用类型"),
            // Rust
            new(QuestionDomain.Rust, "所有权", "所有权与移动"),
            new(QuestionDomain.Rust, "借用", "借用与生命周期"),
            new(QuestionDomain.Rust, "生命周期", "借用与生命周期"),
            new(QuestionDomain.Rust, "Result", "错误处理Result"),
            new(QuestionDomain.Rust, "panic", "错误处理Result"),
            new(QuestionDomain.Rust, "trait", "Trait与泛型"),
            new(QuestionDomain.Rust, "Cargo.toml", "模块系统"),
            new(QuestionDomain.Rust, "crate", "模块系统"),
            // Java
            new(QuestionDomain.Java, "JVM", "JVM内存模型"),
            new(QuestionDomain.Java, "字节码", "JVM内存模型"),
            new(QuestionDomain.Java, "javac", "JVM内存模型"),
            new(QuestionDomain.Java, "泛型", "泛型与擦除"),
            new(QuestionDomain.Java, "接口", "接口与抽象类"),
            new(QuestionDomain.Java, "GC", "JVM内存模型"),
            new(QuestionDomain.Java, "Stream", "Stream API"),
            // JavaScript
            new(QuestionDomain.JavaScript, "Promise", "异步Promise"),
            new(QuestionDomain.JavaScript, "async", "异步Promise"),
            new(QuestionDomain.JavaScript, "闭包", "作用域与闭包"),
            new(QuestionDomain.JavaScript, "let", "作用域与闭包"),
            new(QuestionDomain.JavaScript, "const", "作用域与闭包"),
            new(QuestionDomain.JavaScript, "原型", "原型与继承"),
            new(QuestionDomain.JavaScript, "===", "类型强制转换"),
            new(QuestionDomain.JavaScript, "事件循环", "事件循环"),
            // Go
            new(QuestionDomain.Go, "goroutine", "goroutine调度"),
            new(QuestionDomain.Go, "channel", "channel通信"),
            new(QuestionDomain.Go, "defer", "defer语义"),
            new(QuestionDomain.Go, "interface", "接口隐式实现"),
            new(QuestionDomain.Go, "slice", "切片与映射"),
            new(QuestionDomain.Go, "map", "切片与映射"),
            new(QuestionDomain.Go, "select", "channel通信"),
            // 数据结构
            new(QuestionDomain.DataStructure, "LIFO", "栈的应用"),
            new(QuestionDomain.DataStructure, "FIFO", "队列与双端队列"),
            new(QuestionDomain.DataStructure, "栈", "栈的应用"),
            new(QuestionDomain.DataStructure, "队列", "队列与双端队列"),
            new(QuestionDomain.DataStructure, "二叉", "二叉树遍历"),
            new(QuestionDomain.DataStructure, "哈希", "哈希冲突处理"),
            new(QuestionDomain.DataStructure, "复杂度", "时间复杂度"),
            new(QuestionDomain.DataStructure, "Dijkstra", "图遍历"),
            new(QuestionDomain.DataStructure, "BFS", "图遍历"),
            new(QuestionDomain.DataStructure, "DFS", "图遍历"),
            // 数据库
            new(QuestionDomain.Database, "范式", "范式与冗余"),
            new(QuestionDomain.Database, "事务", "事务ACID"),
            new(QuestionDomain.Database, "隔离", "隔离级别"),
            new(QuestionDomain.Database, "索引", "索引选择"),
            new(QuestionDomain.Database, "主键", "主键与外键"),
            new(QuestionDomain.Database, "外键", "主键与外键"),
            new(QuestionDomain.Database, "DELETE", "查询优化思路"),
            new(QuestionDomain.Database, "JOIN", "JOIN类型"),
            // 操作系统
            new(QuestionDomain.OperatingSystem, "进程", "进程状态切换"),
            new(QuestionDomain.OperatingSystem, "线程", "线程与内核线程"),
            new(QuestionDomain.OperatingSystem, "死锁", "死锁条件"),
            new(QuestionDomain.OperatingSystem, "页表", "分页与分段"),
            new(QuestionDomain.OperatingSystem, "虚拟内存", "虚拟内存"),
            new(QuestionDomain.OperatingSystem, "调度", "调度算法"),
            new(QuestionDomain.OperatingSystem, "FCFS", "调度算法"),
            // 计算机网络
            new(QuestionDomain.ComputerNetwork, "TCP", "OSI与TCP/IP"),
            new(QuestionDomain.ComputerNetwork, "UDP", "UDP适用场景"),
            new(QuestionDomain.ComputerNetwork, "三次握手", "三次握手"),
            new(QuestionDomain.ComputerNetwork, "HTTP", "HTTP状态码"),
            new(QuestionDomain.ComputerNetwork, "DNS", "DNS解析"),
            new(QuestionDomain.ComputerNetwork, "子网", "子网划分"),
            new(QuestionDomain.ComputerNetwork, "NAT", "NAT概念"),
            new(QuestionDomain.ComputerNetwork, "IPv4", "子网划分"),
            // 未分类 / 通识
            new(QuestionDomain.Uncategorized, "缺陷", "缺陷优先级"),
            new(QuestionDomain.Uncategorized, "需求", "需求变更管理"),
            new(QuestionDomain.Uncategorized, "迭代", "迭代计划"),
            new(QuestionDomain.Uncategorized, "评审", "代码评审要点"),
            new(QuestionDomain.Uncategorized, "版本控制", "版本控制基础"),
            new(QuestionDomain.Uncategorized, "风险", "风险识别"),
            new(QuestionDomain.Uncategorized, "软件", "可维护性")
        };

        RulesOrderedByNeedleLength = rules
            .OrderByDescending(r => r.Needle.Length)
            .ThenBy(r => r.Domain)
            .ToArray();
    }

    /// <summary>
    /// 在题干与选项合并文本中查找首个命中规则，返回对应知识点标签；无命中则返回 null。
    /// </summary>
    public static string? TryMatch(QuestionDomain domain, string haystack)
    {
        if (string.IsNullOrEmpty(haystack))
        {
            return null;
        }

        foreach (var r in RulesOrderedByNeedleLength)
        {
            if (r.Domain != domain)
            {
                continue;
            }

            if (haystack.Contains(r.Needle, StringComparison.OrdinalIgnoreCase))
            {
                return r.Label;
            }
        }

        return null;
    }
}
