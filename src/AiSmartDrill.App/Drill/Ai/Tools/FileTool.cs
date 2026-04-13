using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AiSmartDrill.App.Drill.Ai.Client;

namespace AiSmartDrill.App.Drill.Ai.Tools;

/// <summary>
/// 文件工具请求
/// </summary>
public class FileToolRequest
{
    /// <summary>
    /// 操作类型：read, write 或 search
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 文件内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 搜索模式
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
}

/// <summary>
/// 文件工具，实现文件读取、写入和搜索功能
/// </summary>
public class FileTool : ITool
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name => "file_operation";

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description => "用于读取、写入和搜索文件";

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string parameters)
    {
        try
        {
            var request = JsonSerializer.Deserialize<FileToolRequest>(parameters);
            if (request == null)
            {
                return "参数格式错误";
            }

            // 限制文件访问范围，只允许访问应用程序目录
            var baseDirectory = AppContext.BaseDirectory;
            var fullPath = Path.Combine(baseDirectory, request.Path);

            // 检查路径是否在应用程序目录内，防止路径遍历攻击
            if (!fullPath.StartsWith(baseDirectory))
            {
                return "访问被拒绝：只能访问应用程序目录内的文件";
            }

            switch (request.Action)
            {
                case "read":
                    return await ReadFileAsync(fullPath);
                case "write":
                    return await WriteFileAsync(fullPath, request.Content);
                case "search":
                    return await SearchFilesAsync(baseDirectory, request.Pattern);
                default:
                    return $"不支持的操作: {request.Action}";
            }
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
                Required = new() { "action" },
                Properties = new()
                {
                    { "action", new ParameterProperty { Type = "string", Description = "操作类型：read, write 或 search" } },
                    { "path", new ParameterProperty { Type = "string", Description = "文件路径，当 action 为 read 或 write 时必填" } },
                    { "content", new ParameterProperty { Type = "string", Description = "文件内容，当 action 为 write 时必填" } },
                    { "pattern", new ParameterProperty { Type = "string", Description = "搜索模式，当 action 为 search 时必填" } }
                }
            }
        };
    }

    /// <summary>
    /// 读取文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件内容</returns>
    private async Task<string> ReadFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            return "文件不存在";
        }

        var content = await File.ReadAllTextAsync(path);
        // 限制返回内容长度，避免过多内容
        return content.Length > 2000 ? content.Substring(0, 2000) + "..." : content;
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="content">文件内容</param>
    /// <returns>执行结果</returns>
    private async Task<string> WriteFileAsync(string path, string content)
    {
        // 确保目录存在
        var directory = System.IO.Path.GetDirectoryName(path);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content);
        return "文件写入成功";
    }

    /// <summary>
    /// 搜索文件
    /// </summary>
    /// <param name="directory">搜索目录</param>
    /// <param name="pattern">搜索模式</param>
    /// <returns>搜索结果</returns>
    private async Task<string> SearchFilesAsync(string directory, string pattern)
    {
        var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
        var result = string.Join("\n", files);
        return result.Length > 1000 ? result.Substring(0, 1000) + "..." : result;
    }
}