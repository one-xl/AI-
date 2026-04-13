using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AiSmartDrill.App.Drill.Ai.Config;

namespace AiSmartDrill.App.Drill.Ai.Client;

/// <summary>
/// 豆包 API 客户端，实现 IChatCompletionService 接口
/// </summary>
public class DoubaoApiClient : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly DoubaoModelConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 初始化 <see cref="DoubaoApiClient"/> 的新实例
    /// </summary>
    /// <param name="httpClient">HTTP 客户端</param>
    /// <param name="config">豆包模型配置</param>
    public DoubaoApiClient(HttpClient httpClient, DoubaoModelConfig config)
    {
        _httpClient = httpClient;
        _config = config;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // 配置 HTTP 客户端
        _httpClient.BaseAddress = new Uri(config.BaseUrl);
        _httpClient.Timeout = config.Timeout;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    /// <inheritdoc />
    public async Task<ChatCompletionResponse> GenerateCompletionAsync(
        IList<ChatMessage> messages,
        IList<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _config.ModelName,
            messages = messages,
            tools = tools,
            temperature = 0.7,
            max_tokens = 1000,
            stream = false,
            enable_thinking = _config.EnableThinking
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync("/chat/completions", requestContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions)!;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatCompletionStreamResponse> GenerateCompletionStreamAsync(
        IList<ChatMessage> messages,
        IList<ToolDefinition>? tools = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _config.ModelName,
            messages = messages,
            tools = tools,
            temperature = 0.7,
            max_tokens = 1000,
            stream = true,
            enable_thinking = _config.EnableThinking
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        using var response = await _httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
            {
                Content = requestContent
            },
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // 处理 SSE 格式，移除 "data: " 前缀
            if (line.StartsWith("data: "))
            {
                line = line[6..];
            }

            // 检查是否是结束消息
            if (line == "[DONE]")
            {
                break;
            }

            ChatCompletionStreamResponse? streamResponse = null;
            try
            {
                streamResponse = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(line, _jsonOptions);
            }
            catch (JsonException)
            {
                // 忽略解析错误，继续处理下一行
            }

            if (streamResponse != null)
            {
                yield return streamResponse;
            }
        }
    }
}
