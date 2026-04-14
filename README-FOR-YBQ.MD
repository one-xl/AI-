# AI 智能题库与刷题系统 - 从零开始部署教程

本教程将指导你从零开始完成以下步骤：
1. 下载并安装必要工具（VSCode、Git、.NET SDK）
2. 拉取项目代码到本地
3. 部署并运行项目
4. 在火山引擎官网申请豆包模型 API

---

## 目录
- [快速开始](#快速开始)
- [方法一：使用 winget 终端一键安装所有工具](#方法一使用-winget-终端一键安装所有工具)
- [方法二：手动下载安装所有工具](#方法二手动下载安装所有工具)
- [第三步：拉取项目代码](#第三步拉取项目代码)
- [第四步：配置项目](#第四步配置项目)
- [第五步：运行项目](#第五步运行项目)
- [第六步：申请火山引擎豆包模型 API](#第六步申请火山引擎豆包模型-api)
- [常用快捷键速查](#常用快捷键速查)

---

## 快速开始

根据你的偏好选择下面任一方法安装必要工具，然后继续从「第三步：拉取项目代码」开始。

---

## 方法一：使用 winget 终端一键安装所有工具

如果你使用 Windows 10 1809 及以上版本，推荐使用这种方法，三条命令搞定所有工具！

### 1.1 打开管理员终端

按 `Win + X`，选择 **「Windows PowerShell (管理员)」** 或 **「终端 (管理员)」**。

### 1.2 依次运行安装命令

```powershell
# 安装 VSCode
winget install Microsoft.VisualStudioCode

# 安装 Git
winget install Git.Git

# 安装 .NET 7 SDK
winget install Microsoft.DotNet.SDK.7
```

### 1.3 验证安装

安装完成后，**重启终端** 或 **重启电脑**，然后运行以下命令验证：

```powershell
# 验证 .NET
dotnet --version

# 验证 Git
git --version

# 验证 VSCode（如果有输出说明已安装）
code --version
```

### 1.4 安装 VSCode 扩展

1. 打开 VSCode
2. 按 `Ctrl + Shift + X` 打开扩展面板
3. 搜索并安装：**C# Dev Kit**

---

## 方法二：手动下载安装所有工具

### 2.1 安装 VSCode

1. 访问 VSCode 官网：[https://code.visualstudio.com/](https://code.visualstudio.com/)
2. 点击 **「Download for Windows」** 下载
3. 双击运行安装程序，按向导完成安装（建议勾选「添加到 PATH」）
4. 打开 VSCode，按 `Ctrl + Shift + X` 打开扩展面板，搜索并安装：**C# Dev Kit**

### 2.2 安装 Git

1. 访问 Git 官网：[https://git-scm.com/downloads](https://git-scm.com/downloads)
2. 下载 Git for Windows
3. 双击运行安装程序，按向导完成安装（使用默认设置即可）

### 2.3 安装 .NET 7 SDK

1. 访问 .NET 下载页：[https://dotnet.microsoft.com/download/dotnet/7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
2. 在 **「SDK」** 部分，点击 **「x64」** 下载
3. 双击运行安装程序，按向导完成安装

### 2.4 验证安装

安装完成后，**重启终端**，运行：

```powershell
dotnet --version
git --version
```

---

## 第三步：拉取项目代码

### 3.1 打开 PowerShell

任选一种方式打开 PowerShell：
- 按 `Win + R`，输入 `powershell`，按 `Enter`
- 或直接在你想存放项目的文件夹中，**按住 Shift 并右键点击空白处**，选择 **「在此处打开 PowerShell 窗口」**

### 3.2 进入存放目录

```powershell
# 例如进入用户文档目录
cd ~\Documents
```

### 3.3 克隆项目

```powershell
git clone https://github.com/one-xl/AI-intelligent-question-bank-and-practice-system.git
```

### 3.4 进入项目目录并用 VSCode 打开

```powershell
cd AI-intelligent-question-bank-and-practice-system
code .
```

---

## 第四步：配置项目

### 4.1 配置应用设置

在 VSCode 中：

1. 按 `Ctrl + Shift + E` 打开文件资源管理器
2. 找到 `src/AiSmartDrill.App/appsettings.example.json`
3. 选中该文件，按 `Ctrl + C` 复制，然后按 `Ctrl + V` 粘贴
4. 选中新文件，按 `F2`，重命名为 `appsettings.json`，按 `Enter`

### 4.2 还原 NuGet 包

1. 按 `Ctrl + ~` 打开终端
2. 运行：

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

```powershell
dotnet run --project src/AiSmartDrill.App/AiSmartDrill.App.csproj -c Release
```

或者如果你安装了 Visual Studio：
1. 双击 `AiSmartDrill.sln` 打开
2. 右键 `AiSmartDrill.App` → **「设为启动项目」**
3. 按 `F5` 运行

首次启动会自动初始化数据库并创建演示数据。

---

## 第六步：申请火山引擎豆包模型 API

### 6.1 注册并登录火山引擎

1. 访问火山引擎官网：[https://www.volcengine.com/](https://www.volcengine.com/)
2. 点击右上角 **「注册」**，完成账号注册和实名认证

### 6.2 开通火山方舟服务

1. 登录控制台，搜索 **「火山方舟」** 或 **「Ark」**
2. 进入服务页面，点击 **「立即开通」**

### 6.3 创建 API Key

1. 在火山方舟控制台，找到 **「API 密钥管理」**
2. 点击 **「创建 API Key」**，填写名称，点击 **「确定」**
3. **重要**：立即按 `Ctrl + C` 复制并保存 API Key，只显示一次！

### 6.4 创建模型接入点

1. 找到 **「模型推理」** 或 **「接入点管理」**
2. 点击 **「创建接入点」**，选择豆包模型（推荐 `Doubao-lite-32k`）
3. 填写接入点名称，点击 **「创建」**
4. 等待状态变为 **「运行中」**

### 6.5 获取接入点信息

点击刚创建的接入点，记录：
- **接入点 ID**（`ep-` 开头）
- **API 访问地址**（BaseUrl）

### 6.6 配置项目使用豆包 API

回到 VSCode，编辑 `src/AiSmartDrill.App/appsettings.json`：

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

按 `Ctrl + S` 保存。

**或者使用更安全的用户机密方式**：

在终端运行（按 `Ctrl + ~` 打开终端）：

```powershell
cd src/AiSmartDrill.App
dotnet user-secrets init
dotnet user-secrets set "DoubaoModel:ApiKey" "你的API Key"
dotnet user-secrets set "DoubaoModel:ModelName" "你的接入点ID"
dotnet user-secrets set "DoubaoModel:BaseUrl" "你的API访问地址"
```

### 6.7 测试 API 连接

1. 重新运行项目（终端中按 `Ctrl + C` 停止，再运行启动命令）
2. 尝试使用 AI 功能（如「问 AI」）
3. 正常工作说明配置成功！

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
| `Ctrl + S` | 保存文件 |
| `Ctrl + C` / `Ctrl + V` | 复制/粘贴 |
| `F2` | 重命名 |

### Windows 常用快捷键

| 快捷键 | 功能 |
|--------|------|
| `Win + R` | 打开「运行」对话框 |
| `Win + X` | 打开快捷菜单 |
| `Win + E` | 打开文件资源管理器 |
| `Shift + 右键` | 文件夹中打开 PowerShell |

---

## 常见问题

### Q: 运行时提示找不到 `appsettings.json`？
A: 确保已将 `appsettings.example.json` 复制并重命名为 `appsettings.json`。

### Q: AI 请求失败？
A: 检查 API Key、接入点 ID、BaseUrl 是否正确，接入点状态是否为「运行中」，账户是否有余额。

### Q: 如何避免把 API Key 提交到 Git？
A: `appsettings.json` 已在 `.gitignore` 中，不会被提交。推荐使用用户机密方式。

### Q: winget 命令找不到？
A: winget 是 Windows 10 1809+ 自带的。没有的话可以安装 [App Installer](https://www.microsoft.com/zh-cn/p/app-installer/9nblggh4nns1)，或用手动安装方式。

---

## 下一步

现在你已经成功部署了项目！接下来可以：
1. 探索各项功能
2. 添加自己的题目
3. 使用 AI 辅助学习
4. 查看 [docs/ComplexModules.md](docs/ComplexModules.md) 和 [docs/ImportGuide.md](docs/ImportGuide.md) 了解更多

祝你使用愉快！
