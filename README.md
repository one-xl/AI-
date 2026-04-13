# AI 智能题库与刷题系统（Windows / WPF）

课程项目「AI 智能题库与刷题系统」的可运行实现：**WPF 桌面端 + EF Core + SQLite**，包含题库管理、复合筛选、随机组卷与倒计时考试、自动判分、错题本闭环，以及基于**火山引擎方舟（Ark）API** 的 AI 错题解析、题目推荐与学习计划（可替换为其他实现）。

**仓库地址**：[one-xl/AI-intelligent-question-bank-and-practice-system](https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system)  
**克隆**：`git clone https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system.git`

---

## 功能概览

| 模块 | 说明 |
|------|------|
| 题库管理 | 按题型、难度等条件筛选（AND），支持新建 / 编辑 / 保存 / 删除题目 |
| 考试 / 刷题 | 在筛选后的题池中随机抽题，倒计时，上一题 / 下一题，手动或到时自动交卷 |
| 判分与记录 | 交卷后写入答题记录，并按规则自动判分 |
| 错题本 | 错题聚合展示，支持「错题再练」重新组卷 |
| AI | 错题解析、题目推荐、学习计划；默认走 HTTP 调用方舟 Chat Completions（需在配置中填写密钥与接入点） |

更细的内部设计见 [`docs/ComplexModules.md`](docs/ComplexModules.md)。

---

## 环境要求

- **操作系统**：Windows（需 WPF，目标框架 `net7.0-windows`）
- **SDK**：[.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) 或更高，并安装 **“.NET 桌面开发”** 工作负载（Visual Studio 安装器可选组件）
- **网络**：使用在线 AI 时需能访问火山引擎方舟 API 地址（见配置节）

---

## 快速开始

### 1. 获取代码

```powershell
git clone https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system.git
cd AI-intelligent-question-bank-and-practice-system
```

若本地目录名不同，请进入你克隆后的仓库根目录（内含 `AiSmartDrill.sln`）。

### 2. 还原与编译

在仓库根目录执行：

```powershell
dotnet restore AiSmartDrill.sln
dotnet build AiSmartDrill.sln -c Release
```

### 3. 配置文件（必做）

应用启动时会读取输出目录下的 **`appsettings.json`**（该文件因含敏感信息已加入 `.gitignore`，不会从仓库拉取）。

请在本机执行：

1. 复制 `src/AiSmartDrill.App/appsettings.example.json`
2. 在同目录下另存为 **`appsettings.json`**
3. 编辑 `appsettings.json`：
   - **`ConnectionStrings:Default`**：SQLite 路径；默认 `Data Source=AiSmartDrill.db` 表示数据库文件与程序输出目录同级（随 `dotnet run` 或 VS 调试输出位置变化）
   - **`DoubaoModel:ApiKey`**：替换为你的方舟 API Key（勿提交到 Git）
   - **`DoubaoModel:ModelName`**：替换为你的推理接入点 ID（一般为 `ep-...`）
   - **`DoubaoModel:BaseUrl`**：一般为 `https://ark.cn-beijing.volces.com/api/v3/`（与控制台地域一致即可）

也可使用 **用户机密**（推荐开发机）避免明文落在磁盘上被误提交：

```powershell
cd src/AiSmartDrill.App
dotnet user-secrets set "DoubaoModel:ApiKey" "你的密钥"
dotnet user-secrets set "DoubaoModel:ModelName" "你的 ep-接入点"
```

用户机密与 `appsettings.json` 会由配置系统合并；机密优先级更高。

### 4. 运行

```powershell
dotnet run --project src/AiSmartDrill.App/AiSmartDrill.App.csproj -c Release
```

或在 Visual Studio 中打开 `AiSmartDrill.sln`，将 **`AiSmartDrill.App`** 设为启动项目后按 **F5**。

首次启动会初始化数据库并写入演示种子数据。

### 5. NuGet 源

若本机只配置了离线源，请使用仓库根目录的 [`NuGet.config`](NuGet.config)（已指向 `https://api.nuget.org/v3/index.json`），或在 Visual Studio / `dotnet` 中恢复为官方源后再 `dotnet restore`。

---

## 使用指南（界面操作）

### 题库管理

1. 在 **「题库管理」** 区域选择 **题型**、**难度** 等筛选条件（多条件为 **同时满足**）。
2. 点击 **「刷新题库」** 加载列表。
3. 选中一行后，在右侧编辑题干、选项、标准答案等。
4. 使用 **新建 / 保存 / 删除** 维护题目；保存后再次刷新可确认结果。

### 考试 / 刷题

1. 在相同筛选条件下设置 **题量** 与 **考试时长（秒）**。
2. 点击 **「随机组卷并开始」**：从当前筛选池内随机抽取指定数量的题目。
3. 作答过程中使用 **「上一题」/「下一题」** 切换；顶部显示剩余时间。
4. **「交卷」** 手动提交；倒计时为 **0** 时会自动交卷（应用内已做防重复提交处理）。

### 判分与错题本

1. 交卷后系统写入答题记录并 **自动判分**（规则见下一节）。
2. 错题会进入 **错题本**（演示场景下用户固定为 `DatabaseInitializer.DemoUserId`）。
3. 打开 **「错题本 / AI」** 相关区域，**刷新** 可查看错题列表。
4. 使用 **「错题再练（组卷）」** 可仅针对错题再次随机组卷练习。

### AI 功能

1. **交卷后若存在错题**，会尝试调用 **错题解析**（`IAiTutorService`），结果展示在界面文本区域。
2. **「AI 题目推荐」**：根据当前上下文推荐后续练习题。
3. **「生成 AI 学习计划」**：生成阶段性学习安排说明。

若未正确配置 `DoubaoModel`（缺少 ApiKey 或 ModelName），启动日志中会有警告，调用方舟接口时可能失败；请检查密钥、接入点与网络。

---

## 判分规则（摘要）

与 [`docs/ComplexModules.md`](docs/ComplexModules.md) 中描述一致，便于答题时自检：

- **单选题**：与标准答案字符串比较，**忽略大小写**。
- **多选题**：按分隔符拆分选项后 **排序再比较**，忽略顺序与大小写。
- **判断题**：将常见真/假写法归一为「对 / 错」再比较。
- **简答题**：将标准答案拆成多个 **关键词**，要求用户答案 **全部包含**（演示策略，非语义模型）。

---

## 常见问题

**Q：克隆后编译通过，一运行就报找不到 `appsettings.json`？**  
A：请按上文 **「配置文件（必做）」** 在 `src/AiSmartDrill.App/` 下创建 `appsettings.json`（可从 `appsettings.example.json` 复制）。

**Q：数据库文件在哪里？**  
A：由连接字符串决定。默认 `AiSmartDrill.db` 与 **可执行文件输出目录** 相同；用 VS 调试时一般在 `bin\Debug\net7.0-windows\` 下。

**Q：AI 一直失败？**  
A：核对 `DoubaoModel:ApiKey`、`DoubaoModel:ModelName`（`ep-...`）、`BaseUrl` 是否与火山控制台一致；查看调试输出窗口中的异常或日志。

**Q：不要把密钥推到 GitHub？**  
A：`appsettings.json` 已在 `.gitignore` 中；仅提交 **`appsettings.example.json`** 中的占位符即可。

---

## 解决方案结构

- **`src/AiSmartDrill.App`**：WPF 界面、ViewModel、领域模型、EF Core、`DatabaseInitializer` 与种子、`Drill.*` 下的考试 / 判分 / 导入 / **AI（Ark 客户端与 API 服务实现）**。

命名空间使用 `AiSmartDrill.App.Drill.*` 存放业务与 AI 相关代码，避免与 `System.Windows.Application` 冲突。

---

## 许可证与课程说明

本项目为课程项目基线代码，便于本地运行与二次开发。使用第三方 AI 服务时请遵守相应平台的服务条款与计费说明。
