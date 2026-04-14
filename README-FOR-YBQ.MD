# AI 智能题库与刷题系统 - 从零开始部署教程

本教程将指导你从零开始完成以下步骤：
1. 下载并安装 Visual Studio Code (VSCode)
2. 拉取项目代码到本地
3. 部署并运行项目
4. 在火山引擎官网申请豆包模型 API

---

## 目录
- [第一步：下载并安装 VSCode](#第一步下载并安装-vscode)
- [第二步：安装 .NET SDK](#第二步安装-net-sdk)
- [第三步：拉取项目代码](#第三步拉取项目代码)
- [第四步：配置项目](#第四步配置项目)
- [第五步：运行项目](#第五步运行项目)
- [第六步：申请火山引擎豆包模型 API](#第六步申请火山引擎豆包模型-api)

---

## 第一步：下载并安装 VSCode

### 1.1 下载 VSCode

1. 访问 VSCode 官方网站：[https://code.visualstudio.com/](https://code.visualstudio.com/)
2. 点击页面上的 **「Download for Windows」** 按钮（系统会自动识别你的操作系统）
3. 等待下载完成

### 1.2 安装 VSCode

1. 找到下载的安装文件（通常在 `下载` 文件夹中），文件名类似 `VSCodeUserSetup-x64-1.x.x.exe`
2. 双击运行安装程序
3. 按照安装向导的提示进行安装：
   - 点击 **「我同意此协议」**，然后点击 **「下一步」**
   - 选择安装路径（可以使用默认路径），点击 **「下一步」**
   - 选择开始菜单文件夹，点击 **「下一步」**
   - 勾选你需要的附加任务（建议勾选 **「添加到 PATH」**），点击 **「下一步」**
   - 点击 **「安装」** 开始安装
4. 安装完成后，点击 **「完成」** 启动 VSCode

### 1.3 安装必要的 VSCode 扩展

1. 打开 VSCode
2. 点击左侧侧边栏的 **「扩展」** 图标（或按 `Ctrl+Shift+X`）
3. 搜索并安装以下扩展：
   - **C# Dev Kit** - 用于 C# 开发
   - **.NET Core Test Explorer** - 用于运行 .NET 测试（可选）

---

## 第二步：安装 .NET SDK

本项目需要 .NET 7 SDK 或更高版本。

### 2.1 下载 .NET SDK

1. 访问 .NET 官方下载页面：[https://dotnet.microsoft.com/download/dotnet/7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
2. 在 **「SDK」** 部分，点击 **「x64」**（或根据你的系统选择相应版本）下载 .NET 7 SDK
3. 等待下载完成

### 2.2 安装 .NET SDK

1. 找到下载的安装文件，双击运行
2. 按照安装向导的提示完成安装
3. 安装完成后，打开命令提示符（CMD）或 PowerShell，运行以下命令验证安装：

```powershell
dotnet --version
```

如果显示版本号（例如 `7.0.x`），说明安装成功。

---

## 第三步：拉取项目代码

### 3.1 安装 Git（如果尚未安装）

1. 访问 Git 官方网站：[https://git-scm.com/downloads](https://git-scm.com/downloads)
2. 下载并安装 Git for Windows
3. 安装完成后，打开命令提示符，运行以下命令验证安装：

```powershell
git --version
```

### 3.2 克隆项目仓库

1. 打开命令提示符或 PowerShell
2. 进入你想存放项目的目录，例如：

```powershell
cd C:\Users\你的用户名\Documents
```

3. 运行以下命令克隆项目：

```powershell
git clone https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system.git
```

4. 进入项目目录：

```powershell
cd AI-intelligent-question-bank-and-practice-system
```

---

## 第四步：配置项目

### 4.1 用 VSCode 打开项目

1. 打开 VSCode
2. 点击 **「文件」** → **「打开文件夹」**
3. 选择刚才克隆的项目文件夹 `AI-intelligent-question-bank-and-practice-system`
4. 点击 **「选择文件夹」**

### 4.2 配置应用设置

1. 在 VSCode 的文件资源管理器中，找到 `src/AiSmartDrill.App/appsettings.example.json` 文件
2. 右键点击该文件，选择 **「复制」**
3. 在同一目录下右键点击，选择 **「粘贴」**
4. 将新文件重命名为 `appsettings.json`

### 4.3 还原 NuGet 包

1. 在 VSCode 中打开终端（按 `Ctrl+` ` 或点击 **「终端」** → **「新建终端」**）
2. 运行以下命令还原 NuGet 包：

```powershell
dotnet restore AiSmartDrill.sln
```

---

## 第五步：运行项目

### 5.1 编译项目

在终端中运行以下命令编译项目：

```powershell
dotnet build AiSmartDrill.sln -c Release
```

### 5.2 运行项目

运行以下命令启动应用：

```powershell
dotnet run --project src/AiSmartDrill.App/AiSmartDrill.App.csproj -c Release
```

或者，如果你安装了 Visual Studio（推荐）：

1. 双击 `AiSmartDrill.sln` 文件用 Visual Studio 打开
2. 将 `AiSmartDrill.App` 设为启动项目
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
5. **重要**：创建后请立即复制并保存你的 API Key，因为它只会显示一次！

### 6.4 创建模型接入点（Endpoint）

1. 在火山方舟控制台，找到 **「模型推理」** 或 **「接入点管理」**
2. 点击 **「创建接入点」** 或 **「新建接入点」**
3. 选择你想使用的豆包模型（例如：`Doubao-pro-32k`、`Doubao-lite` 等）
4. 填写接入点名称（例如：`my-doubao-endpoint`）
5. 配置其他参数（如推理规格、计费方式等）
6. 点击 **「创建」** 或 **「确定」**
7. 等待接入点状态变为 **「运行中」**

### 6.5 获取接入点信息

1. 在接入点列表中，点击你刚创建的接入点
2. 找到并记录以下信息：
   - **接入点 ID**（通常以 `ep-` 开头）
   - **API 访问地址**（BaseUrl）
   - 确认模型名称

### 6.6 配置项目使用豆包 API

回到 VSCode，编辑 `src/AiSmartDrill.App/appsettings.json` 文件：

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

**或者使用用户机密方式（更安全，推荐）：**

在 VSCode 终端中运行：

```powershell
cd src/AiSmartDrill.App
dotnet user-secrets init
dotnet user-secrets set "DoubaoModel:ApiKey" "你的API Key"
dotnet user-secrets set "DoubaoModel:ModelName" "你的接入点ID"
dotnet user-secrets set "DoubaoModel:BaseUrl" "你的API访问地址"
```

### 6.7 测试 API 连接

1. 重新运行项目
2. 尝试使用 AI 相关功能（如「问 AI」、「AI 题目推荐」等）
3. 如果功能正常工作，说明 API 配置成功！

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

---

## 下一步

现在你已经成功部署了项目并配置了豆包 API！接下来可以：

1. 探索项目的各项功能
2. 在题库管理中添加你自己的题目
3. 使用 AI 功能辅助学习
4. 查看 [docs/ComplexModules.md](docs/ComplexModules.md) 了解更多技术细节
5. 查看 [docs/ImportGuide.md](docs/ImportGuide.md) 了解如何导入题库

祝你使用愉快！
