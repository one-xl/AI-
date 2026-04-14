# AI 智能题库与刷题系统（Windows / WPF）

基于 **WPF + EF Core + SQLite** 的桌面端课程项目：题库管理、复合筛选、随机组卷与倒计时考试、自动判分、错题本闭环，以及通过 **OpenAI 兼容 Chat Completions** 接入大模型（当前实现面向火山引擎方舟，可在 DI 中替换为其他 `IChatCompletionService`）。

| 项 | 链接 |
|----|------|
| 仓库 | [one-xl/AI-intelligent-question-bank-and-practice-system](https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system) |
| 克隆 | `git clone https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system.git` |

---

## 功能概览

| 模块 | 说明 |
|------|------|
| **题库管理** | 按题型、难度等条件 **AND** 筛选；新建 / 编辑 / 保存 / 删除；客观题选项在表格中可读展示 |
| **考试 / 刷题** | 在当前筛选池内随机抽题；倒计时；上一题 / 下一题；手动或到时自动交卷（带防重复提交） |
| **判分与记录** | 交卷写入答题记录并自动判分 |
| **错题本** | 错题聚合；支持错题再练组卷；与 AI 解析联动 |
| **AI** | 错题解析、题目推荐、学习计划、考试中单题讲解等；HTTP 调用 Chat Completions；支持 **进行中取消** 请求 |
| **模型连接** | 支持 **多模型档案**（配置中 `Profiles` 或界面「增加模型档案」）；根节 `ApiKey` / `ModelName` / `BaseUrl` 可被档案覆盖 |
| **界面与主题** | 明 / 暗主题；可选按 **日出 / 日落** 自动切换（需配置观测点经纬度） |

更细的内部设计见 [`docs/ComplexModules.md`](docs/ComplexModules.md)；题库导入说明见 [`docs/ImportGuide.md`](docs/ImportGuide.md)。

---

## 技术栈

- **UI**：WPF（`net7.0-windows`）、CommunityToolkit.Mvvm  
- **数据**：Entity Framework Core 7、SQLite  
- **配置与依赖注入**：`Microsoft.Extensions.*`（Configuration、Options、Http、Logging）  
- **日志**：Serilog 文件输出（按项目既有配置）  
- **AI**：自研 `ArkChatCompletionClient`（相对路径 `chat/completions` + 可配置的根 BaseUrl）

---

## 环境要求

- **系统**：Windows（WPF 仅支持 Windows 桌面）  
- **SDK**：[.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) 及以上；建议安装 Visual Studio **「.NET 桌面开发」** 工作负载  
- **网络**：使用在线 AI 时需能访问你在配置中填写的 **BaseUrl** 所在网络（公网或企业内网由你的服务商决定）

---

## 快速开始

### 1. 获取代码

```powershell
git clone https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system.git
cd AI-intelligent-question-bank-and-practice-system
```

若本地文件夹名不同，请进入包含 `AiSmartDrill.sln` 的仓库根目录。

### 2. 还原与编译

```powershell
dotnet restore AiSmartDrill.sln
dotnet build AiSmartDrill.sln -c Release
```

### 3. 本地配置（必做）

应用从输出目录读取 **`appsettings.json`**。该文件含密钥与连接信息，**已列入 `.gitignore`，不会从 Git 拉取**。

1. 复制 `src/AiSmartDrill.App/appsettings.example.json`  
2. 在同目录保存为 **`appsettings.json`**  
3. 按下面「配置项说明」填写 `DoubaoModel` 等节  

**推荐开发环境使用用户机密**，避免明文长期落在可被误提交的目录：

```powershell
cd src/AiSmartDrill.App
dotnet user-secrets set "DoubaoModel:ApiKey" "<你的 API Key>"
dotnet user-secrets set "DoubaoModel:ModelName" "<接入点 ID 或模型名>"
dotnet user-secrets set "DoubaoModel:BaseUrl" "<控制台文档给出的根路径，通常以 /api/v3/ 结尾>"
```

用户机密与 `appsettings.json` 由配置系统合并，**机密优先级更高**。

### 4. 运行

```powershell
dotnet run --project src/AiSmartDrill.App/AiSmartDrill.App.csproj -c Release
```

或在 Visual Studio 中打开 `AiSmartDrill.sln`，将 **AiSmartDrill.App** 设为启动项目后按 **F5**。  
首次启动会初始化数据库并写入演示种子数据。

### 5. NuGet 源

若本机只配置了离线源，请使用仓库根目录的 [`NuGet.config`](NuGet.config)，或将 `dotnet` / Visual Studio 的包源恢复为 **nuget.org** 后再执行 `dotnet restore`。

---

## 配置项说明

### `ConnectionStrings:Default`

SQLite 连接串。默认 `Data Source=AiSmartDrill.db` 表示数据库文件与 **可执行文件输出目录** 同级（`dotnet run` 或 VS 调试时一般在 `bin\Debug\net7.0-windows\` 下）。

### `DoubaoModel`（方舟 OpenAI 兼容）

| 键 | 说明 |
|----|------|
| `ApiKey` | 服务商颁发的 API Key（勿提交到 Git） |
| `ModelName` | 请求体中的 `model` 字段：推理接入点 ID（常见为 `ep-...`）或文档允许的模型 ID |
| `BaseUrl` | Chat Completions 的 **根地址**（到 `/api/v3/` 为止）。若误粘贴带 `.../chat/completions` 的完整 URL，客户端会截断多余路径段 |
| `ActiveProfileId` | 多档案时的当前选用键（默认 `default`） |
| `Profiles` | 可选：命名多组连接，每组可只写与根节不同的字段 |

若 `BaseUrl` 或 `ModelName` 为空，应用可在界面提示；真正发起请求前客户端会校验并抛出可读错误，**仓库内不提供任何个人端点或密钥默认值**。

### `SunSchedule`（可选）

控制是否按本地日出/日落自动切换明、暗主题（与顶栏「跟随日出日落」开关及用户偏好文件配合）。

| 键 | 说明 |
|----|------|
| `EnableAutoTheme` | 是否启用自动主题 |
| `Latitude` / `Longitude` | 观测点经纬度（北纬、东经为正） |

未在 `appsettings.json` 中配置时使用代码内默认观测点；极区日期可能出现无法计算日出日落的情况，界面会给出提示，可改经纬度或关闭自动。

---

## 使用指南（界面）

### 题库管理

1. 在 **题库管理** 区选择题型、难度等条件（多条件同时满足），**刷新题库**。  
2. 选中一行，在右侧编辑题干、选项、答案等，使用 **新建 / 保存 / 删除** 维护数据。

### 考试 / 刷题

1. 在相同筛选条件下设置 **题量** 与 **考试时长**。  
2. **随机组卷并开始** → 作答 → **上一题 / 下一题** → **交卷**；倒计时到 0 会触发自动交卷。

### 错题本与 AI

1. 交卷后错题进入错题本；可 **刷新** 列表、**错题再练** 组卷。  
2. **AI 题目推荐**、**学习计划**、考试中的 **问 AI** 等会调用已配置的模型；长时间任务可使用界面上的 **取消** 中止请求。

### 主题

使用顶栏主题切换；若启用日出日落策略，日落后为深色、日出后为浅色（具体以配置与偏好为准）。

---

## 判分规则（摘要）

与 [`docs/ComplexModules.md`](docs/ComplexModules.md) 一致：

- **单选题**：与标准答案比较，忽略大小写。  
- **多选题**：选项拆分后排序再比较，忽略顺序与大小写。  
- **判断题**：常见真/假写法归一为「对 / 错」再比较。  
- **简答题**：标准答案拆成多个关键词，要求用户答案 **全部包含**（演示策略，非语义模型）。

---

## 解决方案结构

| 路径 | 说明 |
|------|------|
| `src/AiSmartDrill.App` | WPF 界面、ViewModel、EF Core、`DatabaseInitializer` 与种子数据 |
| `src/AiSmartDrill.App/Drill` | 考试、判分、题库与 **AI 服务**（HTTP 客户端、DTO、接口实现） |

命名空间 `AiSmartDrill.App.Drill.*` 用于业务与 AI，避免与 `System.Windows.Application` 冲突。

---

## 常见问题

**Q：克隆后编译成功，运行提示找不到 `appsettings.json`？**  
A：在 `src/AiSmartDrill.App/` 下从 `appsettings.example.json` 复制并改名为 `appsettings.json`，至少补全 `DoubaoModel` 中密钥、模型名与 BaseUrl。

**Q：数据库文件在哪？**  
A：由连接字符串决定；默认与可执行文件同目录的 `AiSmartDrill.db`。

**Q：AI 请求失败？**  
A：检查 `ApiKey`、`ModelName`、`BaseUrl` 是否与服务商控制台一致；查看日志或调试输出中的 HTTP 状态与错误正文。

**Q：如何避免把密钥推到 GitHub？**  
A：不要 `git add` 本地的 `appsettings.json`；仓库已 `.gitignore` 该文件。仅维护带占位符的 `appsettings.example.json`。

---

## 许可证与声明

本项目为课程项目基线代码，便于本地运行与二次开发。使用第三方 AI 服务时，请遵守对应平台的服务条款、隐私政策与计费规则。
