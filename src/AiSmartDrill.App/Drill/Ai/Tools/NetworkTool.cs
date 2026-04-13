using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AiSmartDrill.App.Drill.Ai.Client;

namespace AiSmartDrill.App.Drill.Ai.Tools;

/// <summary>
/// 网络工具请求
/// </summary>
public class NetworkToolRequest
{
    /// <summary>
    /// 操作类型：search 或 fetch
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 搜索查询
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 网页 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// 网络工具，实现网络搜索和网页抓取功能
/// </summary>
public class NetworkTool : ITool
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name => "network_search";

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description => "用于进行网络搜索和网页抓取";

    /// <summary>
    /// 初始化 <see cref="NetworkTool"/> 的新实例
    /// </summary>
    /// <param name="httpClient">HTTP 客户端</param>
    public NetworkTool(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string parameters)
    {
        try
        {
            var request = JsonSerializer.Deserialize<NetworkToolRequest>(parameters);
            if (request == null)
            {
                return "参数格式错误";
            }

            switch (request.Action)
            {
                case "search":
                    return await SearchAsync(request.Query);
                case "fetch":
                    return await FetchAsync(request.Url);
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
                    { "action", new ParameterProperty { Type = "string", Description = "操作类型：search 或 fetch" } },
                    { "query", new ParameterProperty { Type = "string", Description = "搜索查询，当 action 为 search 时必填" } },
                    { "url", new ParameterProperty { Type = "string", Description = "网页 URL，当 action 为 fetch 时必填" } }
                }
            }
        };
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    /// <param name="query">搜索查询</param>
    /// <returns>搜索结果</returns>
    private async Task<string> SearchAsync(string query)
    {
        // 使用 DuckDuckGo API 进行搜索
        var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    /// <summary>
    /// 抓取网页内容
    /// </summary>
    /// <param name="url">网页 URL</param>
    /// <returns>网页内容</returns>
    private async Task<string> FetchAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        // 限制返回内容长度，避免过多内容
        return content.Length > 2000 ? content.Substring(0, 2000) + "..." : content;
    }
}