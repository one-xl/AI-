using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using AiSmartDrill.App.Drill.Ai.Client;

namespace AiSmartDrill.App.Drill.Ai.Tools;

/// <summary>
/// 命令工具请求
/// </summary>
public class CommandToolRequest
{
    /// <summary>
    /// 命令名称
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 命令参数
    /// </summary>
    public string[] Args { get; set; } = Array.Empty<string>();
}

/// <summary>
/// 命令工具，在沙箱中执行安全命令
/// </summary>
public class CommandTool : ITool
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name => "command_execution";

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description => "用于在沙箱中执行安全命令";

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string parameters)
    {
        try
        {
            var request = JsonSerializer.Deserialize<CommandToolRequest>(parameters);
            if (request == null)
            {
                return "参数格式错误";
            }

            // 检查命令是否安全
            if (!IsSafeCommand(request.Command))
            {
                return "命令执行被拒绝：该命令被认为是不安全的";
            }

            // 执行命令
            return await ExecuteCommandAsync(request.Command, request.Args);
        }
        catch (Exception ex)
        {
            return $"执行失败: {ex.Message}";
        }
    }

    /// <inheritdoc />
    public ToolDefinition GetToolDefinition()
    {
        return new ToolDefinition
        {
            Name = Name,
            Description = Description,
            Parameters = new ToolParameters
            {
                Type = "object",
                Required = new() { "command" },
                Properties = new()
                {
                    { "command", new ParameterProperty { Type = "string", Description = "命令名称" } },
                    { "args", new ParameterProperty { Type = "array", Description = "命令参数" } }
                }
            }
        };
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

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <param name="args">命令参数</param>
    /// <returns>命令执行结果</returns>
    private async Task<string> ExecuteCommandAsync(string command, string[] args)
    {
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
}