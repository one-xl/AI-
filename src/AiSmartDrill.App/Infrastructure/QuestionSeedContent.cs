using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App.Infrastructure;

/// <summary>
/// 种子题库中各题型的具体题干片段、选项文本与标准答案（与 <see cref="QuestionSeedBuilder"/> 的 variant 规则配套）。
/// </summary>
internal static class QuestionSeedContent
{
    /// <summary>
    /// 将题号 1..20 映射到 0..3 四个轮换模板（与题型在循环中的出现顺序一致）。
    /// </summary>
    public static int Variant(int questionIndex1Based) => (questionIndex1Based - 1) / 5;

    /// <summary>
    /// 单选题：返回正确选项字母与四条选项文案（已含 A./B./ 前缀）。
    /// </summary>
    public static (char Correct, string[] Options) SingleChoice(QuestionDomain domain, int v) => domain switch
    {
        QuestionDomain.Python => Pick(v,
            ('A', "A. 使用 def 定义函数", "B. 代码块必须用花括号界定", "C. 元组不是序列类型", "D. 列表字面量使用圆括号创建"),
            ('B', "A. GIL 只存在于解释型语言", "B. CPython 实现中 C 层存在全局解释器锁", "C. 多线程下 CPU 密集型任务总能线性加速", "D. asyncio 会创建与 CPU 核数相等的 OS 线程"),
            ('C', "A. import 只能导入标准库", "B. 模块名必须与文件名不同", "C. 包目录下常用 __init__.py 参与命名空间", "D. from 子句不可与 import 同用"),
            ('D', "A. venv 只能装在 Linux", "B. virtualenv 不是 Python 生态工具", "C. 虚拟环境会复制整个操作系统", "D. venv 模块可创建隔离的 Python 环境")),
        QuestionDomain.C => Pick(v,
            ('A', "A. printf 的格式串由实现解析", "B. C 没有指针类型", "C. struct 成员默认 private", "D. 数组名在表达式中永不可退化为指针"),
            ('B', "A. malloc 返回已清零内存", "B. free 传入非 malloc/calloc/realloc 得到的指针行为未定义", "C. realloc 不可改变原块大小", "D. void* 不可与别的指针比较"),
            ('C', "A. static 局部变量存储期与自动变量相同", "B. static 全局符号默认对其他翻译单元可见", "C. static 局部变量只初始化一次且生命周期贯穿程序", "D. static 函数形参表示堆分配"),
            ('D', "A. 头文件会参与链接符号解析", "B. #include 在预处理之后执行", "C. 头文件不可包含类型声明", "D. 头文件常用于声明接口而定义放 .c")),
        QuestionDomain.CPlusPlus => Pick(v,
            ('A', "A. class 默认 public 继承", "B. 析构函数不可为虚函数", "C. RAII 与资源释放无关", "D. new 返回的一定不是指针"),
            ('B', "A. 引用必须重新绑定到另一对象", "B. 引用在声明时必须绑定到已存在对象", "C. 引用占用与指针不同大小的存储", "D. 右值引用不能延长临时对象生命周期"),
            ('C', "A. unique_ptr 可复制", "B. shared_ptr 不引用计数", "C. unique_ptr 表达独占所有权", "D. auto_ptr 仍是现代推荐首选"),
            ('D', "A. 模板必须在 .cpp 分离实例化无限制", "B. typename 与 class 在模板形参中永不等价", "C. 模板实参不可为类型", "D. 函数模板可参与重载解析")),
        QuestionDomain.CSharp => Pick(v,
            ('A', "A. struct 默认可为 null 的引用类型", "B. string 是值类型", "C. int 是引用类型", "D. 值类型实例常分配在栈或内联于托管对象"),
            ('B', "A. async 方法必在同一线程完成", "B. await 可挂起异步方法直到任务完成", "C. Task 只能表示 CPU 工作", "D. async void 被推荐用于所有公共 API"),
            ('C', "A. 接口可包含字段实现", "B. record 不可用于不可变数据建模", "C. 接口可定义成员签名由实现类提供", "D. sealed 类必须被继承"),
            ('D', "A. LINQ 只能查询内存集合", "B. IEnumerable 不可延迟执行", "C. 查询表达式与扩展方法链无关", "D. LINQ to Objects 对 IEnumerable 提供扩展方法")),
        QuestionDomain.Rust => Pick(v,
            ('A', "A. 所有权可在运行期随意共享可变别名", "B. 借用检查在链接阶段完成", "C. 可变借用与不可变借用可无限重叠", "D. 编译器通过所有权与生命周期规则避免部分数据竞争"),
            ('B', "A. Option 只能表示错误", "B. Result<T,E> 用于可恢复错误传递", "C. panic! 永不展开栈", "D. ? 只能用于 main"),
            ('C', "A. cargo 不能构建测试", "B. crate 与包概念无关", "C. Cargo.toml 描述依赖与元数据", "D. rustc 不能单独调用"),
            ('D', "A. trait 不能定义默认实现", "B. impl Trait 仅用于返回具体类型名", "C. dyn Trait 与对象安全无关", "D. trait 对象通过 dyn Trait 在运行期分发")),
        QuestionDomain.Java => Pick(v,
            ('A', "A. Java 源码直接生成机器码", "B. JVM 与字节码无关", "C. javac 输出 class 含字节码", "D. JIT 从不参与热点优化"),
            ('B', "A. 接口在 Java 8 前可有默认方法", "B. 接口可声明 static 工具方法（高版本）", "C. 接口不能有私有方法", "D. 接口不能多实现"),
            ('C', "A. 泛型在运行期擦除后仍保留 List<int>", "B. 通配符 ? 与边界无关", "C. 泛型提供编译期类型检查", "D. 原始类型 raw type 在现代代码中推荐"),
            ('D', "A. GC 只回收栈帧", "B. finalize 保证立即回收", "C. 强引用不可达对象永不可回收", "D. 可达性分析是常见 GC 根扫描策略")),
        QuestionDomain.JavaScript => Pick(v,
            ('A', "A. const 绑定不可变意味着对象字段也不可改", "B. let 具有块级作用域", "C. var 在 ES6 后推荐使用于循环", "D. TDZ 与 let/const 无关"),
            ('B', "A. === 会进行类型转换", "B. === 不做类型转换的相等比较", "C. == 从不转换类型", "D. Object.is 与 === 永远等价"),
            ('C', "A. Promise 只能同步解析", "B. then 不可链式调用", "C. async 函数返回 Promise", "D. await 只能用于浏览器"),
            ('D', "A. 原型链与继承无关", "B. class 语法糖不基于 prototype", "C. new 与构造函数无关", "D. 对象可通过原型链查找属性")),
        QuestionDomain.Go => Pick(v,
            ('A', "A. goroutine 是 OS 线程一一对应", "B. channel 不能用于同步", "C. go 关键字启动 goroutine", "D. select 只能监听一个 channel"),
            ('B', "A. interface{} 已移除", "B. 隐式实现接口（结构化类型）", "C. 接口必须由嵌入显式声明", "D. Go 无接口类型"),
            ('C', "A. defer 按注册顺序执行", "B. defer 在 return 之后不可运行", "C. defer 常用于关闭资源", "D. defer 只能用于 main"),
            ('D', "A. map 并发写安全", "B. slice 与数组完全相同", "C. make 只能创建 map", "D. slice 是对底层数组的视图")),
        QuestionDomain.DataStructure => Pick(v,
            ('A', "A. 队列是 LIFO", "B. 栈是 FIFO", "C. 栈是 LIFO", "D. 双端队列没有两端"),
            ('B', "A. 二叉搜索树中序遍历无序", "B. BST 有序中序遍历得到排序序列（无重复破坏时）", "C. 堆一定是完全二叉树且键无序", "D. 图没有顶点和边"),
            ('C', "A. 哈希表查找平均 O(n)", "B. 开放定址不会冲突", "C. 好的哈希函数降低冲突概率", "D. 链地址法不使用链表"),
            ('D', "A. Dijkstra 可处理负权边", "B. BFS 不用于无权最短路", "C. DFS 不能用于拓扑排序检测环", "D. 邻接表常用于稀疏图存储")),
        QuestionDomain.Database => Pick(v,
            ('A', "A. DELETE 不带 WHERE 删除全表行", "B. TRUNCATE 一定可回滚", "C. DROP TABLE 只删数据保留表结构", "D. UPDATE 不能改多列"),
            ('B', "A. 第一范式允许非原子字段", "B. 2NF 消除非主属性对候选键的部分依赖", "C. BCNF 弱于 3NF", "D. 反范式永不使用"),
            ('C', "A. 事务 ACID 中 I 表示隔离性", "B. 隔离级别与锁无关", "C. READ UNCOMMITTED 可避免脏读", "D. 幻读与范围锁无关"),
            ('D', "A. 主键可空", "B. 外键不引用他表", "C. UNIQUE 约束允许多行 NULL（依实现）且主键可重复", "D. 主键唯一标识一行")),
        QuestionDomain.OperatingSystem => Pick(v,
            ('A', "A. 进程是资源分配单位之一", "B. 线程从不共享地址空间", "C. 内核线程与用户线程一一对应", "D. PCB 与进程无关"),
            ('B', "A. 死锁四条件缺一仍可能死锁", "B. 互斥、占有且等待、不可抢占、循环等待可构成经典模型", "C. 银行算法不避免死锁", "D. 资源分配图不能检测死锁"),
            ('C', "A. 虚拟内存与分页无关", "B. TLB 不加速地址转换", "C. 页表将虚拟页映射到物理帧", "D. 缺页中断从不发生"),
            ('D', "A. FCFS 总是最优", "B. 时间片轮转不用于分时", "C. 多级反馈队列与优先级无关", "D. 短作业优先可能饥饿长作业")),
        QuestionDomain.ComputerNetwork => Pick(v,
            ('A', "A. TCP 无连接", "B. UDP 必须三次握手", "C. TCP 提供可靠传输", "D. 端口号与进程绑定无关"),
            ('B', "A. IPv4 地址 128 位", "B. 子网掩码区分网络与主机部分", "C. NAT 不修改地址", "D. CIDR 与前缀长度无关"),
            ('C', "A. HTTP 基于 UDP", "B. GET 语义不可缓存", "C. HTTP 常运行于 TCP 之上", "D. HTTPS 不加密"),
            ('D', "A. DNS 只返回 IPv6", "B. ARP 解析域名", "C. ICMP 只用于 ping", "D. DNS 将主机名映射到地址记录")),
        QuestionDomain.Uncategorized => Pick(v,
            ('A', "A. 版本控制可追踪变更", "B. Git 不能分支", "C. 合并永不产生冲突", "D. commit 不保存快照"),
            ('B', "A. 单元测试应依赖真实外部网络", "B. 持续集成强调频繁集成与自动化验证", "C. 代码审查降低质量", "D. 静态分析不能发现空引用"),
            ('C', "A. REST 必须使用 SOAP", "B. HTTP 动词与资源操作无关", "C. REST 常用 HTTP 动词表达资源操作", "D. JSON 不是常见交换格式"),
            ('D', "A. 需求基线永不改变", "B. 敏捷禁止迭代", "C. 瀑布模型禁止文档", "D. 风险登记册用于识别与跟踪风险")),
        _ => Pick(v,
            ('A', "A. 选项一（正确）", "B. 选项二", "C. 选项三", "D. 选项四"),
            ('B', "A. 选项一", "B. 选项二（正确）", "C. 选项三", "D. 选项四"),
            ('C', "A. 选项一", "B. 选项二", "C. 选项三（正确）", "D. 选项四"),
            ('D', "A. 选项一", "B. 选项二", "C. 选项三", "D. 选项四（正确）"))
    };

    /// <summary>
    /// 多选题：标准答案为逗号分隔选项键；选项四条。
    /// </summary>
    public static (string Standard, string[] Options) MultipleChoice(QuestionDomain domain, int v) => domain switch
    {
        QuestionDomain.Python => PickMc(v,
            ("A,C", "A. 列表是可变序列", "B. 元组元素一定可变", "C. dict 的键需可哈希", "D. str 是可变类型"),
            ("B,D", "A. pip 来自标准库 builtins", "B. 可用 venv 创建隔离环境", "C. Python 没有异常机制", "D. with 语句常用于上下文管理"),
            ("A,D", "A. 列表推导可构造新列表", "B. is 与 == 永远等价", "C. GIL 影响部分多线程 CPU 密集型场景", "D. 生成器可惰性求值"),
            ("A,B", "A. PEP 8 是常见风格指南", "B. 模块即 .py 文件可作为命名空间单元", "C. Python 没有包概念", "D. 解释器不读取 .pyc")),
        QuestionDomain.C => PickMc(v,
            ("A,C", "A. sizeof 在编译期求值（对类型/大部分表达式）", "B. void 指针不可转换", "C. 指针算术依赖所指类型大小", "D. 数组下标从 1 开始"),
            ("B,D", "A. const int* 表示指针本身不可变", "B. int const* 与 const int* 含义相关", "C. volatile 用于多线程内存序", "D. 头文件 guard 可避免重复包含"),
            ("A,D", "A. struct 可组合数据成员", "B. union 成员同时全部有效", "C. enum 常量在 C 中不占名空间", "D. typedef 可创建别名"),
            ("B,C", "A. main 返回 void 是标准强制", "B. 函数指针可回调", "C. static 链接性可限制符号可见", "D. inline 强制内联由标准保证")),
        QuestionDomain.CSharp => PickMc(v,
            ("A,C", "A. record 可简化不可变数据建模", "B. 值类型永不在堆上", "C. Span<T> 可表示连续内存视图", "D. delegate 与函数指针无关"),
            ("B,D", "A. LINQ 只能用于 SQL Server", "B. IEnumerable 可延迟执行", "C. property 不能有访问器", "D. using 可释放 IDisposable"),
            ("A,D", "A. async/await 基于状态机变换", "B. Task.Result 永不阻塞", "C. ConfigureAwait(false) 影响同步上下文", "D. ValueTask 可减少分配"),
            ("A,B", "A. Nullable&lt;T&gt; 表示值类型可空", "B. pattern matching 可简化类型分支", "C. switch 表达式不可用", "D. init-only setter 运行期任意赋值")),
        QuestionDomain.Database => PickMc(v,
            ("A,C", "A. 主键唯一且非空", "B. 外键必须自引用", "C. 索引可加速查询但增加写入成本", "D. 视图物理存储所有数据"),
            ("B,D", "A. LEFT JOIN 只保留左表行", "B. INNER JOIN 按连接条件匹配", "C. UNION ALL 自动去重", "D. GROUP BY 与聚合函数配合"),
            ("A,D", "A. 事务隔离级别影响并发异常", "B. 脏读在 READ COMMITTED 必不发生", "C. 锁粒度与性能无关", "D. 死锁可检测并回滚牺牲事务"),
            ("B,C", "A. BCNF 允许主属性对非键部分依赖", "B. 3NF 消除非主属性对候选键的传递依赖", "C. 规范化减少冗余", "D. 反范式永不提升读性能")),
        QuestionDomain.DataStructure => PickMc(v,
            ("A,C", "A. 二分查找要求有序序列", "B. 链表随机访问 O(1)", "C. 堆可用于优先队列", "D. 邻接矩阵适合稀疏图"),
            ("B,D", "A. 快排最坏 O(n log n)", "B. 归并排序稳定且最坏 O(n log n)", "C. 计数排序与元素范围无关", "D. 哈希插入平均 O(1)"),
            ("A,D", "A. 图 BFS 可求无权最短路", "B. 拓扑排序必有环", "C. Kruskal 求最大生成树", "D. 并查集可判连通性"),
            ("A,B", "A. AVL 通过旋转保持平衡", "B. 红黑树近似平衡", "C. BST 插入序不影响树高", "D. Trie 只适合数字")),
        QuestionDomain.ComputerNetwork => PickMc(v,
            ("A,C", "A. TCP 首部有序列号", "B. UDP 提供可靠传输", "C. 三次握手建立连接", "D. 端口号 16 字节"),
            ("B,D", "A. IPv6 地址 32 位", "B. CIDR 用前缀表示网络", "C. NAT 只用于 IPv6", "D. ARP 解析 IPv4 到 MAC"),
            ("A,D", "A. HTTP/1.1 默认可流水线（实现相关）", "B. HTTPS 不校验证书", "C. Cookie 不可跨域", "D. DNS 查询可走 UDP"),
            ("B,C", "A. ICMP 只用于 traceroute", "B. 子网划分减少广播域", "C. VLAN 在二层隔离广播", "D. 路由器工作于物理层")),
        QuestionDomain.OperatingSystem => PickMc(v,
            ("A,C", "A. 进程有独立地址空间映像", "B. 线程从不共享打开文件表", "C. 上下文切换有开销", "D. 用户态与内核态无特权级差异"),
            ("B,D", "A. 银行算法用于页面置换", "B. 死锁预防可破坏互斥", "C. 所有资源都可抢占", "D. 哲学家就餐可限制拿叉顺序"),
            ("A,D", "A. 虚拟内存允许大于物理内存的地址空间", "B. 页表项不含存在位", "C. TLB miss 必崩溃", "D. 工作集模型缓解抖动"),
            ("B,C", "A. FCFS 利于短作业", "B. 多级队列可隔离交互与批处理", "C. 优先级反转需协议缓解", "D. 实时调度忽略截止时间")),
        QuestionDomain.CPlusPlus => PickMc(v,
            ("A,C", "A. 析构函数可声明为虚函数", "B. new/delete 与 malloc/free 完全等价", "C. RAII 绑定资源生命周期到对象", "D. 模板实参不可为整型常量"),
            ("B,D", "A. std::move 总是拷贝", "B. 右值引用可绑定到临时对象", "C. std::unique_ptr 可复制", "D. noexcept 影响移动优化路径"),
            ("A,D", "A. std::vector 连续存储元素", "B. std::list 随机访问 O(1)", "C. std::map 必为哈希表", "D. std::unordered_map 平均 O(1) 查找"),
            ("A,B", "A. const 成员函数可重载非常量版本", "B. explicit 抑制隐式转换构造", "C. operator 不可重载", "D. friend 破坏封装故禁用")),
        QuestionDomain.Rust => PickMc(v,
            ("A,C", "A. match 必须穷尽", "B. Option 与 Result 相同", "C. unsafe 块可绕过部分检查", "D. 生命周期标注改变运行期行为"),
            ("B,D", "A. Box 只能栈分配", "B. Rc 用于单线程引用计数", "C. Arc 不加锁跨线程", "D. Mutex&lt;T&gt; 提供互斥"),
            ("A,D", "A. Iterator::map 惰性", "B. collect 必同步", "C. iter() 获取所有权", "D. into_iter 消耗集合"),
            ("B,C", "A. mod 仅用于二进制 crate", "B. pub 控制可见性", "C. use 可重导出", "D. crate 根只能是 main.rs")),
        QuestionDomain.Java => PickMc(v,
            ("A,C", "A. String 对象不可变", "B. StringBuilder 线程安全", "C. equals 与 == 含义不同", "D. 包装类型永不可空"),
            ("B,D", "A. 接口不能有默认方法", "B. Stream API 可链式中间操作", "C. Optional.get 永不抛异常", "D. try-with-resources 自动关闭"),
            ("A,D", "A. synchronized 可互斥", "B. volatile 替代一切锁", "C. 线程池只创建一个线程", "D. happens-before 定义可见性顺序"),
            ("A,B", "A. List 与 Set 都继承 Collection", "B. Map 不属于 Collection", "C. HashMap 按键有序", "D. TreeMap 哈希实现")),
        QuestionDomain.JavaScript => PickMc(v,
            ("A,C", "A. Promise.all 全成功才 resolved", "B. async 函数不能 await", "C. fetch 返回 Promise", "D. 回调地狱无法缓解"),
            ("B,D", "A. var 有块级作用域", "B. const 绑定不可重新赋值", "C. let 会提升且无 TDZ", "D. 模板字符串用反引号"),
            ("A,D", "A. 原型链用于属性查找", "B. class 字段不可私有", "C. Symbol 总是字符串", "D. Map 键可为对象"),
            ("B,C", "A. JSON.parse 可执行任意代码", "B. 严格模式改变部分静默错误", "C. 模块化有 ES module", "D. CommonJS 仅浏览器")),
        QuestionDomain.Go => PickMc(v,
            ("A,C", "A. channel 可带缓冲", "B. select 只能随机", "C. goroutine 轻量", "D. go test 不能跑基准"),
            ("B,D", "A. error 接口只能 string", "B. errors.Is 可包装链判断", "C. panic 必 recover", "D. defer LIFO"),
            ("A,D", "A. make 用于 slice/map/channel", "B. new 返回值类型", "C. append 必分配新数组", "D. copy 复制切片元素"),
            ("B,C", "A. gofmt 仅格式化注释", "B. go vet 做静态检查", "C. race detector 查数据竞争", "D. module 版本不可语义化")),
        QuestionDomain.Uncategorized => PickMc(v,
            ("A,C", "A. 版本控制可追踪历史变更", "B. 代码审查必然降低交付速度", "C. 自动化测试可回归验证", "D. 需求文档从不需要维护"),
            ("B,D", "A. 敏捷禁止固定节奏会议", "B. CI 在合并前运行检查", "C. 静态分析不能发现任何问题", "D. 代码规范有助于可读性"),
            ("A,D", "A. REST 常用 HTTP 动词建模资源", "B. GraphQL 与 HTTP 无关", "C. JSON 常用于数据交换", "D. 幂等设计有助于安全重试"),
            ("B,C", "A. 风险登记册从不更新", "B. 里程碑用于阶段验收", "C. 干系人沟通计划属于项目管理", "D. 质量管理仅指测试阶段")),
        _ => PickMc(v,
            ("A,C", "A. 选项一正确", "B. 选项二错误", "C. 选项三正确", "D. 选项四错误"),
            ("B,D", "A. 错", "B. 对", "C. 错", "D. 对"),
            ("A,D", "A. 对", "B. 错", "C. 错", "D. 对"),
            ("A,B", "A. 对", "B. 对", "C. 错", "D. 错"))
    };

    /// <summary>
    /// 简答题：题干 + 关键词标准答案（分号分隔表示全部命中）。
    /// </summary>
    public static (string Stem, string Keywords) ShortAnswer(QuestionDomain domain, int v, int index) =>
        (ShortStem(domain, v, index), ShortKeys(domain, v));

    /// <summary>
    /// 填空题：题干（含空格提示）+ 判分用正则。
    /// </summary>
    public static (string Stem, string Regex) FillBlank(QuestionDomain domain, int v, int index) =>
        (FillStem(domain, v, index), FillRegex(domain, v));

    private static string ShortStem(QuestionDomain domain, int v, int index) => domain switch
    {
        QuestionDomain.Python => PickS(v,
            $"【Python】第{index}题（简答）：写出官方包索引站点域名（不含协议）。",
            $"【Python】第{index}题（简答）：写出常用交互式解释器命令（小写四个字母）。",
            $"【Python】第{index}题（简答）：写出列表排序就地的实例方法名（小写）。",
            $"【Python】第{index}题（简答）：写出声明匿名函数的关键字（小写）。"),
        QuestionDomain.C => PickS(v,
            $"【C】第{index}题（简答）：写出程序入口函数名（小写）。",
            $"【C】第{index}题（简答）：写出头文件包含预处理指令（小写，带井号）。",
            $"【C】第{index}题（简答）：写出申请内存的库函数名（小写）。",
            $"【C】第{index}题（简答）：写出释放 malloc 内存的函数名（小写）。"),
        QuestionDomain.CSharp => PickS(v,
            $"【C#】第{index}题（简答）：写出用于异步等待的关键字（小写）。",
            $"【C#】第{index}题（简答）：写出基元整型之一（如 int、long 任选其一，小写）。",
            $"【C#】第{index}题（简答）：写出定义接口类型的关键字（小写）。",
            $"【C#】第{index}题（简答）：写出 LINQ 延迟执行常见的可枚举接口名（含 I 前缀）。"),
        QuestionDomain.CPlusPlus => PickS(v,
            $"【C++】第{index}题（简答）：写出标准输入流对象名（小写）。",
            $"【C++】第{index}题（简答）：写出标准命名空间名（小写）。",
            $"【C++】第{index}题（简答）：写出独占智能指针模板名（小写，含下划线）。",
            $"【C++】第{index}题（简答）：写出常用的 C++ 标准库头文件之一（带 .h 或无扩展，写全名）。"),
        QuestionDomain.Rust => PickS(v,
            $"【Rust】第{index}题（简答）：写出包管理构建工具命令名（小写）。",
            $"【Rust】第{index}题（简答）：写出所有权转移常用方法前缀（小写五个字母）。",
            $"【Rust】第{index}题（简答）：写出可恢复错误常用枚举名（首字母大写）。",
            $"【Rust】第{index}题（简答）：写出不可变引用的符号（一个字符）。"),
        QuestionDomain.Java => PickS(v,
            $"【Java】第{index}题（简答）：写出源码文件扩展名（小写）。",
            $"【Java】第{index}题（简答）：写出字节码文件扩展名（小写）。",
            $"【Java】第{index}题（简答）：写出并发同步关键字之一（小写）。",
            $"【Java】第{index}题（简答）：写出集合框架顶层接口之一（List/Set/Map 任选其一，按实际拼写）。"),
        QuestionDomain.JavaScript => PickS(v,
            $"【JavaScript】第{index}题（简答）：写出 Node 包描述文件名（小写）。",
            $"【JavaScript】第{index}题（简答）：写出声明块级作用域变量的关键字（小写）。",
            $"【JavaScript】第{index}题（简答）：写出定义异步函数的关键字（小写）。",
            $"【JavaScript】第{index}题（简答）：写出浏览器中用于调试输出的常用 API 名（小写，含点）。"),
        QuestionDomain.Go => PickS(v,
            $"【Go】第{index}题（简答）：写出模块元数据文件名（小写，含扩展）。",
            $"【Go】第{index}题（简答）：写出启动轻量协程的关键字（小写）。",
            $"【Go】第{index}题（简答）：写出用于等待一组 goroutine 的类型名（首字母大写）。",
            $"【Go】第{index}题（简答）：写出切片内置函数之一（append 或 len 任选，小写）。"),
        QuestionDomain.DataStructure => PickS(v,
            $"【数据结构】第{index}题（简答）：写出后进先出线性结构的常用中文名（两个字）。",
            $"【数据结构】第{index}题（简答）：写出先进先出线性结构的常用中文名（两个字）。",
            $"【数据结构】第{index}题（简答）：写出二叉树先根遍历的另一种常见叫法（含「序」字）。",
            $"【数据结构】第{index}题（简答）：写出哈希冲突处理之一的中文名（链地址法 或 开放定址 任选完整词）。"),
        QuestionDomain.Database => PickS(v,
            $"【数据库】第{index}题（简答）：写出关系数据库常用声明式语言缩写（大写）。",
            $"【数据库】第{index}题（简答）：写出 MySQL 或 PostgreSQL 任一产品名（大小写按官方常用）。",
            $"【数据库】第{index}题（简答）：写出事务 ACID 中「一致性」英文首字母（单字母大写）。",
            $"【数据库】第{index}题（简答）：写出主键约束英文（两个单词，小写加下划线）。"),
        QuestionDomain.OperatingSystem => PickS(v,
            $"【操作系统】第{index}题（简答）：写出资源分配与调度常用中文单位（两个字）。",
            $"【操作系统】第{index}题（简答）：写出 CPU 调度发生时的中文名（含「切换」更佳，不少于两字）。",
            $"【操作系统】第{index}题（简答）：写出分页机制中的地址转换辅助硬件缩写（大写三个字母）。",
            $"【操作系统】第{index}题（简答）：写出哲学家就餐问题中争夺的资源（中文两字）。"),
        QuestionDomain.ComputerNetwork => PickS(v,
            $"【计算机网络】第{index}题（简答）：写出 OSI 第七层中文名（含「层」字）。",
            $"【计算机网络】第{index}题（简答）：写出 IPv4 地址点分十进制中分隔符（一个字符）。",
            $"【计算机网络】第{index}题（简答）：写出常见无连接传输协议名（大写）。",
            $"【计算机网络】第{index}题（简答）：写出域名系统缩写（大写）。"),
        _ => PickS(v,
            $"【通识】第{index}题（简答）：写出版本控制系统 Git 的初始化仓库子命令（小写）。",
            $"【通识】第{index}题（简答）：写出敏捷开发中固定长度迭代的中文名（两字）。",
            $"【通识】第{index}题（简答）：写出持续集成的英文缩写（大写）。",
            $"【通识】第{index}题（简答）：写出需求文档常见缩写（大写三个字母）。")
    };

    private static string ShortKeys(QuestionDomain domain, int v) => domain switch
    {
        QuestionDomain.Python => PickS(v, "pypi.org", "ipython", "sort", "lambda"),
        QuestionDomain.C => PickS(v, "main", "#include", "malloc", "free"),
        QuestionDomain.CSharp => PickS(v, "await", "int", "interface", "IEnumerable"),
        QuestionDomain.CPlusPlus => PickS(v, "cin", "std", "unique_ptr", "iostream"),
        QuestionDomain.Rust => PickS(v, "cargo", "clone", "Result", "&"),
        QuestionDomain.Java => PickS(v, "java", "class", "synchronized", "List"),
        QuestionDomain.JavaScript => PickS(v, "package.json", "let", "async", "console.log"),
        QuestionDomain.Go => PickS(v, "go.mod", "go", "WaitGroup", "append"),
        QuestionDomain.DataStructure => PickS(v, "栈", "队列", "先序", "链地址"),
        QuestionDomain.Database => PickS(v, "SQL", "MySQL", "C", "primary key"),
        QuestionDomain.OperatingSystem => PickS(v, "进程", "上下文", "TLB", "筷子"),
        QuestionDomain.ComputerNetwork => PickS(v, "应用层", ".", "UDP", "DNS"),
        _ => PickS(v, "init", "迭代", "CI", "PRD")
    };

    private static string FillStem(QuestionDomain domain, int v, int index) => domain switch
    {
        QuestionDomain.Python => PickS(v,
            $"【Python】第{index}题（填空）：在命令行安装包常用命令是 ____（小写）。",
            $"【Python】第{index}题（填空）：PEP 8 建议每级缩进使用 ____ 个空格（填数字）。",
            $"【Python】第{index}题（简答转填空）：创建虚拟环境常用模块名是 ____（小写四个字母）。",
            $"【Python】第{index}题（填空）：不可变序列类型名是 ____（小写五个字母）。"),
        QuestionDomain.C => PickS(v,
            $"【C】第{index}题（填空）：指针空值常量是 ____（大写）。",
            $"【C】第{index}题（填空）：无符号整型前缀字母是 ____（单字母大写）。",
            $"【C】第{index}题（填空）：标准输出流对象是 ____（小写三个字母）。",
            $"【C】第{index}题（填空）：字符串结束符 ASCII 写法是 ____（反斜杠加字母）。"),
        QuestionDomain.CSharp => PickS(v,
            $"【C#】第{index}题（填空）：可空值类型写法在基元后加 ____（一个符号）。",
            $"【C#】第{index}题（填空）：引用相等常用方法名是 ____（首字母大写驼峰）。",
            $"【C#】第{index}题（填空）：特性放在方括号中的语法叫 ____（两个汉字）。",
            $"【C#】第{index}题（填空）：Main 方法返回类型常写 ____（小写）。"),
        QuestionDomain.CPlusPlus => PickS(v,
            $"【C++】第{index}题（填空）：防止头文件重复包含的预处理指令常以 ____ 开头（如 ifndef，小写）。",
            $"【C++】第{index}题（填空）：标准输出流对象名 ____（小写）。",
            $"【C++】第{index}题（填空）：表示无类型指针的关键字 ____（小写四个字母）。",
            $"【C++】第{index}题（填空）：析构函数名称前的符号是 ____（一个字符）。"),
        QuestionDomain.Rust => PickS(v,
            $"【Rust】第{index}题（填空）：包管理配置文件名 ____（小写，含扩展）。",
            $"【Rust】第{index}题（填空）：不可变引用符号 ____（一个字符）。",
            $"【Rust】第{index}题（填空）：可恢复错误常用枚举 ____（首字母大写）。",
            $"【Rust】第{index}题（填空）：声明宏的关键字以 ____ 开头（小写五个字母）。"),
        QuestionDomain.Java => PickS(v,
            $"【Java】第{index}题（填空）：入口方法名 ____（小写四个字母）。",
            $"【Java】第{index}题（填空）：字节码文件扩展名 ____（小写）。",
            $"【Java】第{index}题（填空）：线程同步关键字 ____（小写）。",
            $"【Java】第{index}题（填空）：比较对象内容常用方法 ____（小写）。"),
        QuestionDomain.JavaScript => PickS(v,
            $"【JavaScript】第{index}题（填空）：声明常量关键字 ____（小写五个字母）。",
            $"【JavaScript】第{index}题（填空）：脚本启用严格模式的完整指令为 ____（小写，含空格）。",
            $"【JavaScript】第{index}题（填空）：定义异步函数关键字 ____（小写五个字母）。",
            $"【JavaScript】第{index}题（填空）：浏览器全局对象名 ____（首字母大写其余小写）。"),
        QuestionDomain.Go => PickS(v,
            $"【Go】第{index}题（填空）：模块文件 ____（小写，含扩展）。",
            $"【Go】第{index}题（填空）：启动协程关键字 ____（小写两个字母）。",
            $"【Go】第{index}题（填空）：切片长度函数 ____（小写三个字母）。",
            $"【Go】第{index}题（填空）：接口类型关键字 ____（小写）。"),
        QuestionDomain.DataStructure => PickS(v,
            $"【数据结构】第{index}题（填空）：LIFO 结构中文常称 ____（两字）。",
            $"【数据结构】第{index}题（填空）：FIFO 结构中文常称 ____（两字）。",
            $"【数据结构】第{index}题（填空）：堆通常用 ____ 结构存储（完全二叉树/数组 填其一，填「完全二叉树」或「数组」）。",
            $"【数据结构】第{index}题（填空）：有向无环图常缩写 ____（大写三个字母）。"),
        QuestionDomain.Database => PickS(v,
            $"【数据库】第{index}题（填空）：删除全表数据且保留表结构常用语句关键字 ____（大写）。",
            $"【数据库】第{index}题（填空）：主键约束关键字 ____（小写两个单词下划线连接）。",
            $"【数据库】第{index}题（填空）：事务提交命令 ____（大写）。",
            $"【数据库】第{index}题（填空）：常见开源关系库 MySQL 默认端口 ____（数字）。"),
        QuestionDomain.OperatingSystem => PickS(v,
            $"【操作系统】第{index}题（填空）：进程调度发生在 ____ 切换时（填「上下文」或完整「上下文切换」均可，按正则）。",
            $"【操作系统】第{index}题（填空）：分页地址转换辅助硬件缩写 ____（大写）。",
            $"【操作系统】第{index}题（填空）：临界区需要 ____（填「互斥」或「互斥锁」按正则）访问。",
            $"【操作系统】第{index}题（填空）：死锁四个必要条件之一：____（填「互斥」或「占有等待」等词，按正则）。"),
        QuestionDomain.ComputerNetwork => PickS(v,
            $"【计算机网络】第{index}题（填空）：HTTP 常用端口 ____（数字）。",
            $"【计算机网络】第{index}题（填空）：HTTPS 在 HTTP 下使用的安全协议缩写 ____（大写）。",
            $"【计算机网络】第{index}题（填空）：IPv4 地址长度 ____ 位（填数字）。",
            $"【计算机网络】第{index}题（填空）：DNS 基于的常见传输 ____（大写，三字母）或 UDP 填 UDP）。"),
        _ => PickS(v,
            $"【通识】第{index}题（填空）：Git 提交命令 ____（小写）。",
            $"【通识】第{index}题（填空）：敏捷固定周期叫 ____（两字中文）。",
            $"【通识】第{index}题（填空）：持续集成缩写 ____（大写）。",
            $"【通识】第{index}题（填空）：产品需求文档缩写 ____（大写）。")
    };

    private static string FillRegex(QuestionDomain domain, int v) => domain switch
    {
        QuestionDomain.Python => PickS(v, "^pip$", "^4$", "^venv$", "^tuple$"),
        QuestionDomain.C => PickS(v, "^NULL$", "^u$", "^stdout$", @"^\\0$"),
        QuestionDomain.CSharp => PickS(v, "^\\?$", "^ReferenceEquals$", "^特性$", "^void$"),
        QuestionDomain.CPlusPlus => PickS(v, "^#ifndef$", "^cout$", "^void$", "^~$"),
        QuestionDomain.Rust => PickS(v, "^Cargo\\.toml$", "^&$", "^Result$", "^macro$"),
        QuestionDomain.Java => PickS(v, "^main$", "^\\.?class$", "^synchronized$", "^equals$"),
        QuestionDomain.JavaScript => PickS(v, "^const$", "^use\\s+strict$", "^async$", "^window$"),
        QuestionDomain.Go => PickS(v, "^go\\.mod$", "^go$", "^len$", "^interface$"),
        QuestionDomain.DataStructure => PickS(v, "^栈$", "^队列$", "^(完全二叉树|数组)$", "^DAG$"),
        QuestionDomain.Database => PickS(v, "^DELETE$", "^primary\\s*key$", "^COMMIT$", "^3306$"),
        QuestionDomain.OperatingSystem => PickS(v, "^(上下文|上下文切换)$", "^TLB$", "^互斥(锁)?$", "^(互斥|占有等待|不可抢占|循环等待)$"),
        QuestionDomain.ComputerNetwork => PickS(v, "^80$", "^TLS$", "^32$", "^(UDP|TCP)$"),
        _ => PickS(v, "^commit$", "^迭代$", "^CI$", "^PRD$")
    };

    private static (char Correct, string[] Options) Pick(int v, (char, string, string, string, string) a, (char, string, string, string, string) b, (char, string, string, string, string) c, (char, string, string, string, string) d)
    {
        var t = v switch { 0 => a, 1 => b, 2 => c, _ => d };
        return (t.Item1, new[] { t.Item2, t.Item3, t.Item4, t.Item5 });
    }

    private static (string Standard, string[] Options) PickMc(int v, (string, string, string, string, string) a, (string, string, string, string, string) b, (string, string, string, string, string) c, (string, string, string, string, string) d)
    {
        var t = v switch { 0 => a, 1 => b, 2 => c, _ => d };
        return (t.Item1, new[] { t.Item2, t.Item3, t.Item4, t.Item5 });
    }

    private static string PickS(int v, string a, string b, string c, string d) => v switch { 0 => a, 1 => b, 2 => c, _ => d };
}
