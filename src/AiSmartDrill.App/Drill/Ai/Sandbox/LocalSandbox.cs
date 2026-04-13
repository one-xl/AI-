using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 本地沙箱，直接在主机执行操作，适用于开发环境
/// </summary>
public class LocalSandbox : ISandbox
{
    private readonly string _baseDirectory;

    /// <summary>
    /// 初始化 <see cref="LocalSandbox"/> 的新实例
    /// </summary>
    public LocalSandbox()
    {
        _baseDirectory = AppContext.BaseDirectory;
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
        // 检查命令是否安全
        if (!IsSafeCommand(command))
        {
            return "命令执行被拒绝：该命令被认为是不安全的";
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var result = output;
        if (!string.IsNullOrEmpty(error))
        {
            result += $"\n错误: {error}";
        }

        result += $"\n退出代码: {process.ExitCode}";
        return result;
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path)
    {
        var fullPath = GetSafePath(path);
        if (!File.Exists(fullPath))
        {
            return "文件不存在";
        }

        var content = await File.ReadAllTextAsync(fullPath);
        return content.Length > 2000 ? content.Substring(0, 2000) + "..." : content;
    }

    /// <inheritdoc />
    public async Task<string> WriteFileAsync(string path, string content)
    {
        var fullPath = GetSafePath(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, content);
        return "文件写入成功";
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string path)
    {
        var fullPath = GetSafePath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path)
    {
        var fullPath = GetSafePath(path);
        return Task.FromResult(Directory.Exists(fullPath));
    }

    /// <inheritdoc />
    public Task<string> CreateDirectoryAsync(string path)
    {
        var fullPath = GetSafePath(path);
        Directory.CreateDirectory(fullPath);
        return Task.FromResult("目录创建成功");
    }

    /// <inheritdoc />
    public Task<string> ListDirectoryAsync(string path)
    {
        var fullPath = GetSafePath(path);
        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult("目录不存在");
        }

        var files = Directory.GetFiles(fullPath);
        var directories = Directory.GetDirectories(fullPath);

        var result = "文件:\n";
        result += string.Join("\n", files);
        result += "\n\n目录:\n";
        result += string.Join("\n", directories);

        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取安全的文件路径，防止路径遍历攻击
    /// </summary>
    /// <param name="path">相对路径</param>
    /// <returns>安全的绝对路径</returns>
    private string GetSafePath(string path)
    {
        var fullPath = Path.Combine(_baseDirectory, path);
        // 检查路径是否在应用程序目录内
        if (!fullPath.StartsWith(_baseDirectory))
        {
            throw new Exception("访问被拒绝：只能访问应用程序目录内的文件");
        }
        return fullPath;
    }

    /// <summary>
    /// 检查命令是否安全
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <returns>命令是否安全</returns>
    private bool IsSafeCommand(string command)
    {
        // 允许的安全命令列表
        var safeCommands = new[] { "echo", "dir", "ls", "pwd", "date", "time" };
        return safeCommands.Contains(command.ToLower());
    }
}