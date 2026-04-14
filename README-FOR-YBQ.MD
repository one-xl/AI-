# AI 智能题库与刷题系统 - 从零开始部署教程

本教程将指导你从零开始完成以下步骤：
1. 下载并安装 Visual Studio Code (VSCode)
2. 拉取项目代码到本地
3. 部署并运行项目
4. 在火山引擎官网申请豆包模型 API

---

## 目录
- [快速开始（推荐）](#快速开始推荐)
- [第一步：下载并安装 VSCode](#第一步下载并安装-vscode)
- [第二步：安装 .NET SDK](#第二步安装-net-sdk)
- [第三步：拉取项目代码](#第三步拉取项目代码)
- [第四步：配置项目](#第四步配置项目)
- [第五步：运行项目](#第五步运行项目)
- [第六步：申请火山引擎豆包模型 API](#第六步申请火山引擎豆包模型-api)
- [常用快捷键速查](#常用快捷键速查)

---

## 快速开始（推荐）

如果你使用 Windows 10/11 并且系统已自带 `winget` 包管理器，可以使用以下命令一键安装所有必要工具：

### 方法一：使用 winget 快速安装（最快）

按 `Win + X`，选择 **「Windows PowerShell (管理员)」** 或 **「终端 (管理员)」**，然后依次运行：

```powershell
# 安装 VSCode
winget install Microsoft.VisualStudioCode

# 安装 Git
winget install Git.Git

# 安装 .NET 7 SDK
winget install Microsoft.DotNet.SDK.7
```

安装完成后，**重启终端** 或 **重启电脑**，然后继续从 [第三步：拉取项目代码](#第三步拉取项目代码) 开始。

---

## 第一步：下载并安装 VSCode

### 1.1 方法一：使用 winget 安装（推荐）

按 `Win + X`，选择 **「Windows PowerShell」** 或 **「终端」**，运行：

```powershell
winget install Microsoft.VisualStudioCode
```

### 1.2 方法二：手动下载安装

1. 访问 VSCode 官方网站：[https://code.visualstudio.com/](https://code.visualstudio.com/)
2. 点击页面上的 **「Download for Windows」** 按钮（系统会自动识别你的操作系统）
3. 等待下载完成

找到下载的安装文件（通常在 `下载` 文件夹中），文件名类似 `VSCodeUserSetup-x64-1.x.x.exe`，双击运行，按向导完成安装。

### 1.3 安装必要的 VSCode 扩展

1. 打开 VSCode
2. 按 `Ctrl + Shift + X` 打开扩展面板（或点击左侧侧边栏的 **「扩展」** 图标）
3. 搜索并安装以下扩展：
   - **C# Dev Kit** - 用于 C# 开发
   - **.NET Core Test Explorer** - 用于运行 .NET 测试（可选）

---

## 第二步：安装 .NET SDK

本项目需要 .NET 7 SDK 或更高版本。

### 2.1 方法一：使用 winget 安装（推荐）

在 PowerShell 或终端中运行：

```powershell
winget install Microsoft.DotNet.SDK.7
```

### 2.2 方法二：手动下载安装

1. 访问 .NET 官方下载页面：[https://dotnet.microsoft.com/download/dotnet/7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
2. 在 **「SDK」** 部分，点击 **「x64」**（或根据你的系统选择相应版本）下载 .NET 7 SDK
3. 双击运行安装程序，按向导完成安装

### 2.3 验证安装

安装完成后，**重启终端**，然后运行以下命令验证：

```powershell
dotnet --version
```

如果显示版本号（例如 `7.0.x`），说明安装成功。

---

## 第三步：拉取项目代码

### 3.1 安装 Git（如果尚未安装）

#### 方法一：使用 winget 安装（推荐）

```powershell
winget install Git.Git
```

#### 方法二：手动下载安装

1. 访问 Git 官方网站：[https://git-scm.com/downloads](https://git-scm.com/downloads)
2. 下载并安装 Git for Windows

### 3.2 克隆项目仓库

1. 按 `Win + R`，输入 `powershell`，按 `Enter` 打开 PowerShell
2. 进入你想存放项目的目录，例如：

```powershell
# 进入用户文档目录
cd ~\Documents
```

或者直接在你想存放项目的文件夹中，**按住 Shift 并右键点击空白处**，选择 **「在此处打开 PowerShell 窗口」**。

3. 运行以下命令克隆项目：

```powershell
git clone https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system.git
```

4. 进入项目目录：

```powershell
cd AI-intelligent-question-bank-and-practice-system
```

5. （可选）直接用 VSCode 打开当前目录：

```powershell
code .
```

---

## 第四步：配置项目

### 4.1 用 VSCode 打开项目

如果你还没有用 VSCode 打开项目：

1. 打开 VSCode
2. 按 `Ctrl + K` 然后按 `Ctrl + O`（或点击 **「文件」** → **「打开文件夹」**）
3. 选择刚才克隆的项目文件夹 `AI-intelligent-question-bank-and-practice-system`
4. 点击 **「选择文件夹」**

### 4.2 配置应用设置

1. 在 VSCode 中，按 `Ctrl + Shift + E` 打开文件资源管理器
2. 找到 `src/AiSmartDrill.App/appsettings.example.json` 文件
3. 右键点击该文件，选择 **「复制」**（或按 `Ctrl + C`）
4. 在空白处右键点击，选择 **「粘贴」**（或按 `Ctrl + V`）
5. 将新文件重命名为 `appsettings.json`：
   - 选中新文件，按 `F2`
   - 输入 `appsettings.json`，按 `Enter`

### 4.3 还原 NuGet 包

1. 在 VSCode 中打开终端：按 `Ctrl + ~`（反引号键，在 Tab 键上方）
2. 运行以下命令还原 NuGet 包：

```powershell
dotnet restore AiSmartDrill.sln
```

---

## 第五步：运行项目

### 5.1 编译项目

在终端中运行：

```powershell
dotnet build AiSmartDrill.sln -c Release
```

### 5.2 运行项目

运行以下命令启动应用：

```powershell
dotnet run --project src/AiSmartDrill.App/AiSmartDrill.App.csproj -c Release
```

或者，如果你安装了 Visual Studio（推荐）：

1. 在文件资源管理器中，双击 `AiSmartDrill.sln` 文件用 Visual Studio 打开
2. 将 `AiSmartDrill.App` 设为启动项目（右键点击项目 → **「设为启动项目」**）
3. 按 `F5` 或点击 **「开始调试」** 按钮运行

首次启动会自动初始化数据库并创建演示数据。

---

## 第六步：申请火山引擎豆包模型 API

### 6.1 注册火山引擎账号

1. 访问火山引擎官网：[https://www.volcengine.com/](https://www.volcengine.com/)
2. 点击右上角的 **「注册」** 按钮
3. 按照提示完成账号注册（可以使用手机号或邮箱）
4. 完成实名认证（这是使用 API 服务的必要步骤）

### 6.2 开通火山方舟服务

1. 登录火山引擎控制台
2. 在搜索框中搜索 **「火山方舟」** 或 **「Ark」**
3. 点击进入 **「火山方舟」** 服务页面
4. 点击 **「立即开通」** 或 **「开通服务」** 按钮
5. 按照提示完成服务开通

### 6.3 创建 API Key

1. 在火山方舟控制台，找到 **「API 密钥管理」** 或类似的选项
2. 点击 **「创建 API Key」** 或 **「新建密钥」**
3. 填写密钥名称（例如：`doubao-api-key`）
4. 点击 **「确定」** 或 **「创建」**
5. **重要**：创建后请立即按 `Ctrl + C` 复制并保存你的 API Key，因为它只会显示一次！

### 6.4 创建模型接入点（Endpoint）

1. 在火山方舟控制台，找到 **「模型推理」** 或 **「接入点管理」**
2. 点击 **「创建接入点」** 或 **「新建接入点」**
3. 选择你想使用的豆包模型（推荐：`Doubao-lite-32k` 或 `Doubao-pro-32k`）
4. 填写接入点名称（例如：`my-doubao-endpoint`）
5. 配置其他参数（如推理规格、计费方式等）
6. 点击 **「创建」** 或 **「确定」**
7. 等待接入点状态变为 **「运行中」**（可能需要几分钟）

### 6.5 获取接入点信息

1. 在接入点列表中，点击你刚创建的接入点
2. 找到并记录以下信息：
   - **接入点 ID**（通常以 `ep-` 开头）
   - **API 访问地址**（BaseUrl）
   - 确认模型名称

### 6.6 配置项目使用豆包 API

回到 VSCode，编辑 `src/AiSmartDrill.App/appsettings.json` 文件：

1. 在文件资源管理器中找到 `appsettings.json`，双击打开
2. 修改 `DoubaoModel` 部分：

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=AiSmartDrill.db"
  },
  "DoubaoModel": {
    "ApiKey": "这里粘贴你的API Key",
    "ModelName": "这里粘贴你的接入点ID",
    "BaseUrl": "这里粘贴你的API访问地址",
    "Temperature": 0.7,
    "MaxTokens": 2048,
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "EnableThinking": true,
    "EnableVision": false
  }
}
```

3. 按 `Ctrl + S` 保存文件

**或者使用用户机密方式（更安全，推荐）：**

在 VSCode 终端中运行（按 `Ctrl + ~` 打开终端）：

```powershell
cd src/AiSmartDrill.App
dotnet user-secrets init
dotnet user-secrets set "DoubaoModel:ApiKey" "你的API Key"
dotnet user-secrets set "DoubaoModel:ModelName" "你的接入点ID"
dotnet user-secrets set "DoubaoModel:BaseUrl" "你的API访问地址"
```

### 6.7 测试 API 连接

1. 重新运行项目（在终端中按 `Ctrl + C` 停止当前运行，然后再次运行启动命令）
2. 尝试使用 AI 相关功能（如「问 AI」、「AI 题目推荐」等）
3. 如果功能正常工作，说明 API 配置成功！

---

## 常用快捷键速查

### VSCode 常用快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl + ~` | 打开/关闭终端 |
| `Ctrl + Shift + E` | 打开文件资源管理器 |
| `Ctrl + Shift + X` | 打开扩展面板 |
| `Ctrl + P` | 快速打开文件 |
| `Ctrl + Shift + P` | 打开命令面板 |
| `Ctrl + K` 然后 `Ctrl + O` | 打开文件夹 |
| `Ctrl + S` | 保存文件 |
| `Ctrl + C` / `Ctrl + V` | 复制/粘贴 |
| `Ctrl + F` | 查找 |
| `F2` | 重命名文件/变量 |
| `Ctrl + /` | 注释/取消注释行 |

### Windows 常用快捷键

| 快捷键 | 功能 |
|--------|------|
| `Win + R` | 打开「运行」对话框 |
| `Win + X` | 打开快捷菜单（包含 PowerShell/终端） |
| `Win + E` | 打开文件资源管理器 |
| `Ctrl + Shift + Esc` | 打开任务管理器 |
| `Alt + Tab` | 切换窗口 |

---

## 常见问题

### Q: 运行时提示找不到 `appsettings.json`？
A: 确保你已经将 `appsettings.example.json` 复制并重命名为 `appsettings.json`。

### Q: AI 请求失败怎么办？
A: 检查以下几点：
1. API Key 是否正确
2. ModelName（接入点 ID）是否正确
3. BaseUrl 是否正确
4. 接入点状态是否为「运行中」
5. 账户是否有足够的余额

### Q: 如何避免把 API Key 提交到 Git？
A: `appsettings.json` 已经被加入 `.gitignore`，只要你不手动把它加入 Git，就不会被提交。推荐使用用户机密方式存储敏感信息。

### Q: winget 命令找不到怎么办？
A: winget 是 Windows 10 1809 及更高版本自带的。如果没有，可以手动安装 [App Installer](https://www.microsoft.com/zh-cn/p/app-installer/9nblggh4nns1)，或者使用手动下载安装的方式。

---

## 下一步

现在你已经成功部署了项目并配置了豆包 API！接下来可以：

1. 探索项目的各项功能
2. 在题库管理中添加你自己的题目
3. 使用 AI 功能辅助学习
4. 查看 [docs/ComplexModules.md](docs/ComplexModules.md) 了解更多技术细节
5. 查看 [docs/ImportGuide.md](docs/ImportGuide.md) 了解如何导入题库

祝你使用愉快！
