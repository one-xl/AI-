# AiSmartDrill Code Wiki

## 1. 项目概述

**AiSmartDrill** 是一个基于 Windows WPF 技术栈开发的 AI 智能题库与刷题系统，采用 .NET 7.0 框架，使用 EF Core 作为 ORM，SQLite 作为嵌入式数据库。该项目提供了完整的题库管理、随机组卷、自动判分、错题本功能以及 AI 智能服务接口的可替换实现。

### 核心特性
- 完整的题库 CRUD 管理
- 多题型支持（单选、多选、判断、简答）
- 题型与难度复合筛选
- 随机组卷与倒计时考试
- 自动判分系统
- 错题本闭环学习
- AI 错题解析、题目推荐、学习计划（接口化服务）

---

## 2. 项目整体架构

### 2.1 技术栈

| 组件 | 技术选型 | 版本 |
|------|---------|------|
| 框架 | .NET | 7.0 |
| UI 框架 | WPF | - |
| ORM | Entity Framework Core | 7.0.17 |
| 数据库 | SQLite | - |
| MVVM 工具包 | CommunityToolkit.Mvvm | 8.2.2 |
| 依赖注入 | Microsoft.Extensions.DependencyInjection | 7.0.0 |
| 配置管理 | Microsoft.Extensions.Configuration | 7.0.0 |

### 2.2 项目结构

```
/workspace
├── docs/
│   └── ComplexModules.md          # 复杂模块说明文档
├── src/
│   └── AiSmartDrill.App/        # 主项目目录
│       ├── Domain/             # 领域模型层
│       │   ├── AnswerRecord.cs
│       │   ├── AppUser.cs
│       │   ├── DifficultyLevel.cs
│       │   ├── Question.cs
│       │   ├── QuestionType.cs
│       │   └── WrongBookEntry.cs
│       ├── Drill/              # 业务逻辑层
│       │   ├── Ai/             # AI 服务
│       │   │   ├── AiDtos.cs
│       │   │   ├── IAiTutorService.cs
│       │   │   ├── IQuestionRecommendationService.cs
│       │   │   ├── IStudyPlanService.cs
│       │   │   ├── LocalAiTutorService.cs
│       │   │   ├── LocalQuestionRecommendationService.cs
│       │   │   └── LocalStudyPlanService.cs
│       │   └── Grading/        # 判分服务
│       │       └── AnswerGrader.cs
│       ├── Infrastructure/     # 基础设施层
│       │   ├── AppDbContext.cs
│       │   └── DatabaseInitializer.cs
│       ├── ViewModels/         # 视图模型层
│       │   └── MainWindowViewModel.cs
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── MainWindow.xaml
│       ├── MainWindow.xaml.cs
│       ├── AiSmartDrill.App.csproj
│       └── appsettings.json
├── AiSmartDrill.sln
├── NuGet.config
└── README.md
```

### 2.3 架构层次

```
┌─────────────────────────────────────────────────────────┐
│                    UI 层 (WPF)                         │
│  MainWindow.xaml / MainWindow.xaml.cs                    │
└────────────────────────────┬────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────┐
│                ViewModel 层 (MVVM)                        │
│           MainWindowViewModel.cs                           │
└────────────────────────────┬────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
┌───────▼───────┐    ┌──▼──────────┐   ┌─▼────────────┐
│  业务逻辑层    │    │  AI 服务层    │   │  基础设施层   │
│  AnswerGrader  │    │  IAiTutor*   │   │  AppDbContext│
│               │    │  IRecommendation│   │  DatabaseInit  │
└───────────────┘    └─────────────┘   └───────┬───────┘
                                                 │
┌────────────────────────────────────────────────┐
│              领域模型层 (Domain)                  │
│  Question / AnswerRecord / WrongBookEntry / AppUser│
└────────────────────────────────────────────────┘
                                                 │
┌────────────────────────────────────────────────┐
│              数据持久层 (EF Core + SQLite)   │
└────────────────────────────────────────────────┘
```

---

## 3. 主要模块职责

### 3.1 领域模型层 (Domain)

#### 3.1.1 [Question](file:///workspace/src/AiSmartDrill.App/Domain/Question.cs)
- **职责**: 表示题库中的一道题目，包含题干、标准答案、选项、知识点标签等核心信息
- **核心属性**:
  - `Id`: 题目主键
  - `Type`: 题型（单选/多选/判断/简答）
  - `Difficulty`: 难度等级（简单/中等/困难）
  - `Stem`: 题干文本
  - `StandardAnswer`: 标准答案
  - `OptionsJson`: 选项 JSON 字符串
  - `KnowledgeTags`: 知识点标签（逗号分隔）
  - `IsEnabled`: 是否启用
  - `CreatedAtUtc`: 创建时间

#### 3.1.2 [AnswerRecord](file:///workspace/src/AiSmartDrill.App/Domain/AnswerRecord.cs)
- **职责**: 记录用户的每一次答题详情
- **核心属性**:
  - `Id`: 记录主键
  - `UserId`: 用户外键
  - `QuestionId`: 题目外键
  - `SessionId`: 会话标识（用于聚合同一次考试）
  - `UserAnswer`: 用户作答内容
  - `IsCorrect`: 是否正确
  - `Score`: 得分
  - `DurationMs`: 作答耗时（毫秒）
  - `CreatedAtUtc`: 记录时间

#### 3.1.3 [WrongBookEntry](file:///workspace/src/AiSmartDrill.App/Domain/WrongBookEntry.cs)
- **职责**: 错题本聚合记录，同一用户同一题目在业务上保持唯一
- **核心属性**:
  - `Id`: 主键
  - `UserId`: 用户外键
  - `QuestionId`: 题目外键
  - `WrongCount`: 累计错误次数
  - `LastWrongAtUtc`: 最近一次错误时间

#### 3.1.4 [AppUser](file:///workspace/src/AiSmartDrill.App/Domain/AppUser.cs)
- **职责**: 表示系统中的学习者用户
- **核心属性**:
  - `Id`: 用户主键
  - `DisplayName`: 显示名称
  - `CreatedAtUtc`: 创建时间

#### 3.1.5 [QuestionType](file:///workspace/src/AiSmartDrill.App/Domain/QuestionType.cs)
- **职责**: 题型枚举，定义题目类型
- **枚举值**:
  - `SingleChoice = 0`: 单选题
  - `MultipleChoice = 1`: 多选题
  - `TrueFalse = 2`: 判断题
  - `ShortAnswer = 3`: 简答题

#### 3.1.6 [DifficultyLevel](file:///workspace/src/AiSmartDrill.App/Domain/DifficultyLevel.cs)
- **职责**: 难度等级枚举
- **枚举值**:
  - `Easy = 0`: 简单
  - `Medium = 1`: 中等
  - `Hard = 2`: 困难

### 3.2 基础设施层 (Infrastructure)

#### 3.2.1 [AppDbContext](file:///workspace/src/AiSmartDrill.App/Infrastructure/AppDbContext.cs)
- **职责**: EF Core 数据库上下文，负责实体映射与索引配置
- **核心功能**:
  - 定义 `DbSet<>` 属性映射数据库表
  - 在 `OnModelCreating` 中配置表结构、索引、外键关系
  - 配置字段长度约束
  - 配置复合索引用于高效筛选

#### 3.2.2 [DatabaseInitializer](file:///workspace/src/AiSmartDrill.App/Infrastructure/DatabaseInitializer.cs)
- **职责**: 负责数据库创建与演示种子数据初始化
- **核心功能**:
  - 使用 `EnsureCreatedAsync` 创建数据库结构
  - 写入演示用户数据
  - 构建 18 道演示题目种子数据
  - 预置历史答题记录与错题本条目
  - 提供 `DemoUserId` 常量（1L）

### 3.3 业务逻辑层 (Drill)

#### 3.3.1 [AnswerGrader](file:///workspace/src/AiSmartDrill.App/Drill/Grading/AnswerGrader.cs)
- **职责**: 判分器，将用户答案与标准答案进行题型相关的规范化比较
- **核心方法**:
  - `IsCorrect(Question, string)`: 判断用户答案是否正确
  - `GradeSingleChoice`: 单选题判分（忽略大小写）
  - `GradeMultipleChoice`: 多选题判分（选项规范化后排序比较）
  - `GradeTrueFalse`: 判断题判分（兼容多种真假写法）
  - `GradeShortAnswer`: 简答题判分（关键词命中策略）

#### 3.3.2 AI 服务模块

##### [IAiTutorService](file:///workspace/src/AiSmartDrill.App/Drill/Ai/IAiTutorService.cs)
- **职责**: AI 错题解析服务契约
- **方法**: `AnalyzeWrongQuestionsAsync` - 分析错题列表并返回解析结果

##### [LocalAiTutorService](file:///workspace/src/AiSmartDrill.App/Drill/Ai/LocalAiTutorService.cs)
- **职责**: 本地占位实现的 AI 错题解析服务
- **功能**: 不发起外网请求，基于题型生成模板化解析

##### [IQuestionRecommendationService](file:///workspace/src/AiSmartDrill.App/Drill/Ai/IQuestionRecommendationService.cs)
- **职责**: AI 题目推荐服务契约
- **方法**: `RecommendAsync` - 生成推荐题目列表

##### [IStudyPlanService](file:///workspace/src/AiSmartDrill.App/Drill/Ai/IStudyPlanService.cs)
- **职责**: AI 学习计划生成服务契约
- **方法**: `GeneratePlanAsync` - 生成学习计划

### 3.4 视图模型层 (ViewModels)

#### [MainWindowViewModel](file:///workspace/src/AiSmartDrill.App/ViewModels/MainWindowViewModel.cs)
- **职责**: 主窗口视图模型，聚合所有业务逻辑
- **核心功能**:
  - 题库管理：刷新、新建、保存、删除题目
  - 考试引擎：随机组卷、倒计时、答题、交卷
  - 错题本：刷新、错题再练
  - AI 功能：错题解析、题目推荐、学习计划
  - 使用 `CommunityToolkit.Mvvm` 实现 MVVM 模式

---

## 4. 关键类与函数说明

### 4.1 应用程序入口

#### [App](file:///workspace/src/AiSmartDrill.App/App.xaml.cs)
- **职责**: WPF 应用程序入口
- **核心方法**:
  - `OnStartup`: 构建配置与 DI 容器，初始化数据库并显示主窗口
  - `OnExit`: 释放 DI 容器资源
- **依赖注入配置**:
  - 配置 `IConfiguration`
  - 配置日志
  - 配置 `DbContextFactory<AppDbContext>`
  - 注册 AI 服务（本地实现）
  - 注册 `MainWindowViewModel`

### 4.2 判分逻辑详解

#### AnswerGrader.IsCorrect
```csharp
public static bool IsCorrect(Question question, string? userAnswer)
```
- **功能**: 根据题型选择不同的判分策略
- **参数**:
  - `question`: 题目实体
  - `userAnswer`: 用户作答
- **返回**: 是否正确

#### 判分策略分支

| 题型 | 判分策略 |
|------|---------|
| 单选题 | 忽略大小写比较整串文本 |
| 多选题 | 按分隔符拆分选项后排序比较，忽略顺序与大小写 |
| 判断题 | 将常见真/假写法归一为「对/错」再比较 |
| 简答题 | 将标准答案按分隔符拆成多个关键词，要求用户答案全部包含 |

### 4.3 考试引擎

#### MainWindowViewModel.StartExamAsync
- **功能**: 随机组卷并开始限时考试
- **流程**:
  1. 根据筛选条件获取题库池
  2. 随机打乱顺序后截取前 N 题
  3. 初始化考试状态
  4. 启动倒计时计时器
  5. 渲染第一道题

#### MainWindowViewModel.SubmitExamAsync
- **功能**: 交卷，自动判分、写入记录、更新错题本、触发 AI 解析
- **流程**:
  1. 停止计时器
  2. 遍历所有题目判分
  3. 写入答题记录
  4. 更新错题本（答错时）
  5. 调用 AI 错题解析服务
  6. 显示得分

#### 倒计时机制
- 使用 `DispatcherTimer` 每秒递减剩余秒数
- 到 0 时通过 UI 调度器触发异步交卷
- 使用 `_submitGate`（`Interlocked`）防止并发重复提交

### 4.4 错题本闭环

#### MainWindowViewModel.UpsertWrongBookAsync
- **功能**: 更新错题本聚合记录
- **逻辑**:
  - 如果条目不存在：新增，`WrongCount = 1`
  - 如果条目存在：`WrongCount += 1`，更新 `LastWrongAtUtc`

---

## 5. 依赖关系

### 5.1 NuGet 依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| CommunityToolkit.Mvvm | 8.2.2 | MVVM 工具包，提供 `ObservableObject` 和 `RelayCommand` |
| Microsoft.EntityFrameworkCore.Sqlite | 7.0.17 | EF Core SQLite 提供程序 |
| Microsoft.EntityFrameworkCore.Design | 7.0.17 | EF Core 设计时工具 |
| Microsoft.Extensions.Configuration.Binder | 7.0.0 | 配置绑定 |
| Microsoft.Extensions.Configuration.Json | 7.0.0 | JSON 配置提供程序 |
| Microsoft.Extensions.DependencyInjection | 7.0.0 | 依赖注入容器 |
| Microsoft.Extensions.Logging.Debug | 7.0.0 | 调试日志提供程序 |

### 5.2 内部模块依赖

```
App.xaml.cs
    ↓
MainWindowViewModel
    ├→ AppDbContext (via IDbContextFactory
    ├→ AnswerGrader
    ├→ IAiTutorService
    ├→ IQuestionRecommendationService
    └→ IStudyPlanService

AppDbContext
    ├→ Question
    ├→ AnswerRecord
    ├→ WrongBookEntry
    └→ AppUser

AnswerGrader
    └→ Question
```

---

## 6. 项目运行方式

### 6.1 环境要求

- .NET SDK 7.0 或更高
- Windows 操作系统
- 需要包含 Windows 桌面开发工作负载

### 6.2 安装与运行步骤

1. **还原依赖**
```bash
dotnet restore AiSmartDrill.sln
```

2. **编译项目**
```bash
dotnet build AiSmartDrill.sln -c Release
```

3. **运行应用**
```bash
dotnet run --project src/AiSmartDrill.App/AiSmartDrill.App.csproj -c Release
```

或在 Visual Studio 中打开解决方案后启动 `AiSmartDrill.App`。

### 6.3 数据库

- 首次启动会在输出目录生成 SQLite 文件：`AiSmartDrill.db`
- 演示种子数据在首次创建库时自动写入
- 数据库连接字符串配置在 [appsettings.json](file:///workspace/src/AiSmartDrill.App/appsettings.json)

### 6.4 AI 配置

- [appsettings.json](file:///workspace/src/AiSmartDrill.App/appsettings.json) 中 `Ai:Endpoint` 与 `Ai:ApiKey` 为占位配置
- 当前使用本地实现，无需真实密钥
- 如需替换为真实 AI 服务，实现相应接口即可

---

## 7. 核心业务流程

### 7.1 题库管理流程

```
1. 用户在「题库管理」页选择筛选条件
2. 点击「刷新题库」
3. 选中题目可在右侧编辑
4. 支持「新建」/「保存」/「删除」操作
```

### 7.2 考试流程

```
1. 设置筛选条件、题量与时长
2. 点击「随机组卷并开始」
3. 作答后使用「上一题/下一题」切换
4. 倒计时结束自动交卷，或手动「交卷」
5. 系统自动判分并写入答题记录
6. 错题自动聚合到错题本
```

### 7.3 错题本与 AI 流程

```
1. 在「错题本/AI」页刷新错题列表
2. 可「错题再练（组卷）」
3. 交卷后自动调用 AI 错题解析
4. 可点击「AI 题目推荐」获取推荐
5. 可点击「生成 AI 学习计划」
```

---

## 8. 扩展点与可替换组件

### 8.1 AI 服务替换

当前 AI 服务均为接口化设计，可轻松替换为真实实现：

1. 实现 `IAiTutorService` - 错题解析
2. 实现 `IQuestionRecommendationService` - 题目推荐
3. 实现 `IStudyPlanService` - 学习计划生成
4. 在 [App.xaml.cs](file:///workspace/src/AiSmartDrill.App/App.xaml.cs#L47-L49) 中替换注册

### 8.2 判分策略扩展

[AnswerGrader](file:///workspace/src/AiSmartDrill.App/Drill/Grading/AnswerGrader.cs) 为静态类，可：
- 扩展新题型判分策略
- 替换简答题判分逻辑（当前为关键词命中，可替换为语义相似度）

---

## 9. 配置说明

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=AiSmartDrill.db"
  },
  "Ai": {
    "Endpoint": "https://example.invalid/ai",
    "ApiKey": "PLACEHOLDER_DO_NOT_COMMIT_REAL_KEY"
  }
}
```

- `ConnectionStrings:Default`: SQLite 数据库连接字符串
- `Ai:Endpoint`: AI 服务端点（占位）
- `Ai:ApiKey`: AI 服务 API 密钥（占位）

---

## 10. 注意事项

1. **演示版使用单用户模式（`DemoUserId = 1`）
2. 数据库使用 `EnsureCreated` 而非迁移，生产环境建议改用 `Migrate`
3. AI 服务当前为本地占位实现，不发起外网请求
4. 请勿提交真实 API 密钥到仓库
5. 错题本表对 `(UserId, QuestionId)` 建有唯一索引
6. 题库表对 `(Type, Difficulty, IsEnabled)` 建有复合索引用于高效筛选

---

*本 Code Wiki 文档最后更新：2026-04-11*
