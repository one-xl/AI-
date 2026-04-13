using Systemusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Aiusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Dockerusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox :using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="Containerusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", stringusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerNameusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    publicusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command,using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!Isusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        returnusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsyncusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var argumentsusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} catusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdocusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerNameusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists'using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("existsusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<boolusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDirusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echousing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdocusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerNameusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{pathusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"execusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    ///using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        ifusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latestusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"psusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --formatusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Resultusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行 Docker 命令
    /// </summary>
    /// <param name="arguments">命令参数using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行 Docker 命令
    /// </summary>
    /// <param name="arguments">命令参数</param>
    /// <returns>命令执行结果</returns>
    private async Taskusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行 Docker 命令
    /// </summary>
    /// <param name="arguments">命令参数</param>
    /// <returns>命令执行结果</returns>
    private async Task<string> RunDockerCommandAsync(string arguments)
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行 Docker 命令
    /// </summary>
    /// <param name="arguments">命令参数</param>
    /// <returns>命令执行结果</returns>
    private async Task<string> RunDockerCommandAsync(string arguments)
    {
        var processStartInfo = newusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行 Docker 命令
    /// </summary>
    /// <param name="arguments">命令参数</param>
    /// <returns>命令执行结果</returns>
    private async Task<string> RunDockerCommandAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = argumentsusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行 Docker 命令
    /// </summary>
    /// <param name="arguments">命令参数</param>
    /// <returns>命令执行结果</returns>
    private async Task<string> RunDockerCommandAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Useusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行 Docker 命令
    /// </summary>
    /// <param name="arguments">命令参数</param>
    /// <returns>命令执行结果</returns>
    private async Task<string> RunDockerCommandAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        usingusing System.Diagnostics;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 容器沙箱，使用 Docker 提供隔离环境，适用于生产环境
/// </summary>
public class ContainerSandbox : ISandbox
{
    private readonly string _containerName;
    private readonly string _workingDir;

    /// <summary>
    /// 初始化 <see cref="ContainerSandbox"/> 的新实例
    /// </summary>
    /// <param name="containerName">容器名称</param>
    /// <param name="workingDir">工作目录</param>
    public ContainerSandbox(string containerName = "ai-sandbox", string workingDir = "/app")
    {
        _containerName = containerName;
        _workingDir = workingDir;
        InitializeContainer();
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var arguments = $"exec {_containerName} {command} {string.Join(" ", args)}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var arguments = $"exec {_containerName} cat {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        // 使用 echo 命令写入文件
        var escapedContent = content.Replace("\"", "\\\"");
        var arguments = $"exec {_containerName} sh -c \"echo '{escapedContent}' > {_workingDir}/{path}\"";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -f {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<bool> DirectoryExistsAsync(string path)
    {
        var arguments = $"exec {_containerName} test -d {_workingDir}/{path} && echo 'exists' || echo 'not exists'";
        var result = await RunDockerCommandAsync(arguments);
        return result.Contains("exists");
    }

    /// <inheritdoc />
    public async Task<string> CreateDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} mkdir -p {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <inheritdoc />
    public async Task<string> ListDirectoryAsync(string path)
    {
        var arguments = $"exec {_containerName} ls -la {_workingDir}/{path}";
        return await RunDockerCommandAsync(arguments);
    }

    /// <summary>
    /// 初始化 Docker 容器
    /// </summary>
    private void InitializeContainer()
    {
        // 检查容器是否存在
        var exists = RunDockerCommandAsync($"ps -a --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
        if (!exists.Contains(_containerName))
        {
            // 创建容器
            RunDockerCommandAsync($"run -d --name {_containerName} --rm alpine:latest tail -f /dev/null").Wait();
        }
        else
        {
            // 检查容器是否运行
            var running = RunDockerCommandAsync($"ps --filter name={_containerName} --format '{{{{.Names}}}}'").Result;
            if (!running.Contains(_containerName))
            {
                // 启动容器
                RunDockerCommandAsync($"start {_containerName}").Wait();
            }
        }
    }

    /// <summary>
    /// 运行 Docker 命令
    /// </summary>
    /// <param name="arguments">命令参数</param>
    /// <returns>命令执行结果</returns>
    private async Task<string> RunDockerCommandAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var output = await process.Standard