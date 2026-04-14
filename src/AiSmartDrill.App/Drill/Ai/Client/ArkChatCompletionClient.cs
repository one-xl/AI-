using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiSmartDrill.App.Drill.Ai.Ark;
using AiSmartDrill.App.Drill.Ai.Config;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.Drill.Ai.Client;

/// <summary>
/// 火山方舟豆包文本对话客户端：<c>POST /api/v3/chat/completions</c>，OpenAI 兼容 <c>messages</c>。
/// </summary>
public sealed class ArkChatCompletionClient : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly DoubaoModelConfig _config;
    private readonly JsonSerializerOptions _requestJsonOptions;
    private readonly ILogger<ArkChatCompletionClient> _logger;

    /// <summary>
    /// 初始化方舟 Chat Completions 客户端。
    /// </summary>
    public ArkChatCompletionClient(HttpClient httpClient, DoubaoModelConfig config, ILogger<ArkChatCompletionClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _requestJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _logger.LogInformation("ArkChatCompletionClient 已注册：按请求使用当前模型档案（可切换）。");
    }

    /// <inheritdoc />
    public async Task<ChatCompletionResponse> GenerateCompletionAsync(
        IList<ChatMessage> messages,
        IList<ToolDefinition>? tools = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var snap = _config.GetConnectionSnapshot();
        var request = new
        {
            model = snap.ModelName,
            messages,
            temperature = snap.Temperature,
            max_tokens = maxTokens ?? snap.MaxTokens
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request, _requestJsonOptions),
            Encoding.UTF8,
            "application/json");

        var endpoint = BuildChatCompletionsUri(snap.BaseUrl);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = requestContent };
        ApplyAuthAndAccept(httpRequest, snap);

        _logger.LogInformation("Ark 请求 POST {RequestUri}，profile={Profile} model={Model}", endpoint, _config.ActiveProfileId, snap.ModelName);

        using var response = await _httpClient
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        var traceHeaders = TryGetTraceHeaders(response);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            var formatted = ArkApiErrorParser.FormatHttpError(status, body);
            _logger.LogError("Ark chat/completions 失败：{Detail}，TraceHeaders={Trace}", formatted, traceHeaders);
            throw new InvalidOperationException(formatted);
        }

        var result = JsonSerializer.Deserialize<ChatCompletionResponse>(body, ArkHttpResponseJsonOptions.Value);
        if (result == null)
            throw new InvalidOperationException("Ark 响应反序列化为空（choices 可能缺失）。");

        var usage = result.Usage;
        _logger.LogInformation(
            "Ark 响应 OK：http={HttpStatus} id={Id} respModel={RespModel} totalTokens={Total} trace={Trace}",
            (int)response.StatusCode,
            result.Id,
            result.Model,
            usage?.TotalTokens,
            traceHeaders);

        return result;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatCompletionStreamResponse> GenerateCompletionStreamAsync(
        IList<ChatMessage> messages,
        IList<ToolDefinition>? tools = null,
        int? maxTokens = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var snap = _config.GetConnectionSnapshot();
        var request = new
        {
            model = snap.ModelName,
            messages,
            temperature = snap.Temperature,
            max_tokens = maxTokens ?? snap.MaxTokens,
            stream = true
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request, _requestJsonOptions),
            Encoding.UTF8,
            "application/json");

        var endpoint = BuildChatCompletionsUri(snap.BaseUrl);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = requestContent };
        ApplyAuthAndAccept(httpRequest, snap);

        using var response = await _httpClient
            .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(ArkApiErrorParser.FormatHttpError((int)response.StatusCode, err));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (line.StartsWith("data: ", StringComparison.Ordinal))
                line = line[6..];
            if (line == "[DONE]")
                yield break;

            ChatCompletionStreamResponse? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(line, ArkHttpResponseJsonOptions.Value);
            }
            catch (JsonException)
            {
                // 忽略非 JSON 行
            }

            if (chunk != null)
                yield return chunk;
        }
    }

    private static Uri BuildChatCompletionsUri(string baseUrl)
    {
        var normalized = ArkApiEndpointNormalizer.ToChatCompletionsBaseUrl(baseUrl).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                "方舟 BaseUrl 未配置。请在 appsettings.json、用户机密或「模型档案」中填写控制台给出的 OpenAI 兼容 API 根路径（通常以 /api/v3/ 结尾）。");
        }

        return new Uri($"{normalized}/chat/completions");
    }

    private static void ApplyAuthAndAccept(HttpRequestMessage httpRequest, DoubaoConnectionSnapshot snap)
    {
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", snap.ApiKey);
        httpRequest.Headers.Accept.Clear();
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// 提取响应头中可用于向火山侧对账的追踪字段（名称因网关版本可能不同）。
    /// </summary>
    private static string TryGetTraceHeaders(HttpResponseMessage response)
    {
        foreach (var name in new[] { "x-request-id", "x-trace-id", "request-id", "x-envoy-upstream-service-time" })
        {
            if (response.Headers.TryGetValues(name, out var vals))
                return $"{name}={string.Join(',', vals)}";
        }

        return "-";
    }
}
