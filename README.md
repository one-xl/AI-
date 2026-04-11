# AI 智能题库与刷题系统（Windows / WPF）

本仓库为课程项目「AI 智能题库与刷题系统」的可运行基线：WPF 桌面端 + EF Core + SQLite，覆盖题库 CRUD、题型与难度复合筛选、随机组卷与倒计时考试、自动判分、错题本闭环，以及 AI 错题解析 / 题目推荐 / 学习计划三类**可替换占位服务**（接口 + 本地实现 + 配置占位）。

## 安装说明

1. **环境**：安装 [.NET SDK 7.0](https://dotnet.microsoft.com/download/dotnet/7.0) 或更高（需包含 Windows 桌面开发工作负载；本机已验证 `net7.0-windows`）。
2. **还原与编译**：在仓库根目录执行：
   - `dotnet restore AiSmartDrill.sln`
   - `dotnet build AiSmartDrill.sln -c Release`
3. **运行**：执行 `dotnet run --project src/AiSmartDrill.App/AiSmartDrill.App.csproj -c Release`，或在 Visual Studio 中打开解决方案后启动 `AiSmartDrill.App`。
4. **数据库**：首次启动会在输出目录生成 SQLite 文件（默认见 `src/AiSmartDrill.App/appsettings.json` 中 `ConnectionStrings:Default`）。演示种子数据在首次创建库时写入。
5. **NuGet 源**：若本机曾配置仅离线源，仓库根目录已提供 `NuGet.config` 指向 `https://api.nuget.org/v3/index.json`。
6. **AI 配置**：`appsettings.json` 中 `Ai:Endpoint` 与 `Ai:ApiKey` 仅为占位，**请勿提交真实密钥**。

## 使用说明（主流程）

1. **题库管理**：在「题库管理」页选择题型与难度（AND 筛选），点击「刷新题库」；选中题目可在右侧编辑，支持新建 / 保存 / 删除。
2. **考试 / 刷题**：在同一筛选条件下设置题量与时长，点击「随机组卷并开始」；作答后可用「上一题 / 下一题」切换，倒计时结束会自动交卷，也可手动「交卷」。
3. **判分与错题本**：交卷后写入答题记录；错题自动聚合到错题本（用户维度演示固定为 `DatabaseInitializer.DemoUserId`）。在「错题本 / AI」页刷新列表，并可「错题再练（组卷）」。
4. **AI 功能**：交卷后若有错题，会调用 `IAiTutorService` 生成解析文本；「AI 题目推荐」「生成 AI 学习计划」分别调用推荐与计划占位服务，输出展示于对应文本框。

更细的模块说明见 `docs/ComplexModules.md`。

## 解决方案结构

- `src/AiSmartDrill.App`：WPF 宿主、XAML、ViewModel、领域模型、EF Core 上下文、种子与 AI 占位实现（命名空间 `AiSmartDrill.App.Drill.*` 存放业务与 AI 相关类型，避免与 `System.Windows.Application` 命名冲突）。
