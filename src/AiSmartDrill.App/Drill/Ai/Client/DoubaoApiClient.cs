using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AiSmartDrill.App.Drill.Ai.Config;

namespace AiSmartDrill.App.Drill.Ai.Client
{
    public class DoubaoApiClient : IChatCompletionService
    {
        private readonly HttpClient _httpClient;
        private readonly DoubaoModelConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<DoubaoApiClient> _logger;

        public DoubaoApiClient(HttpClient httpClient, DoubaoModelConfig config, ILogger<DoubaoApiClient> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _logger = logger;

            _httpClient.BaseAddress = new Uri(config.BaseUrl);
            _httpClient.Timeout = config.Timeout;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("DoubaoApiClient 初始化完成");
            _logger.LogInformation("BaseUrl: {BaseUrl}", config.BaseUrl);
            _logger.LogInformation("ModelName: {ModelName}", config.ModelName);
            _logger.LogInformation("ApiKey (前8位): {ApiKeyPrefix}", config.ApiKey.Length >= 8 ? config.ApiKey.Substring(0, 8) + "..." : "***");
        }

        public async Task<ChatCompletionResponse> GenerateCompletionAsync(
            IList<ChatMessage> messages,
            IList<ToolDefinition>? tools = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("开始调用API，消息数: {MessageCount}", messages.Count);
            
            // 使用标准OpenAI兼容格式
            var request = new
            {
                model = _config.ModelName,
                messages = messages,
                temperature = 0.7,
                max_tokens = 1000
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogDebug("请求JSON: {RequestJson}", requestJson);

            var requestContent = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json"
            );

            var fullUrl = new Uri(_httpClient.BaseAddress, "/chat/completions");
            _logger.LogInformation("请求URL: {FullUrl}", fullUrl);

            try
            {
                var response = await _httpClient.PostAsync("/chat/completions", requestContent, cancellationToken);
                _logger.LogInformation("响应状态码: {StatusCode}", (int)response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("响应内容: {ResponseContent}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API调用失败，状态码: {StatusCode}, 响应: {ResponseContent}", 
                        (int)response.StatusCode, responseContent);
                    throw new Exception($"API 调用失败: {response.StatusCode}\n响应内容: {responseContent}");
                }

                var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions)!;
                _logger.LogInformation("API调用成功，选择数: {ChoiceCount}", result.Choices?.Count ?? 0);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP请求异常");
                throw new Exception($"API 调用失败: {ex.Message}", ex);
            }
        }

        public async IAsyncEnumerable<ChatCompletionStreamResponse> GenerateCompletionStreamAsync(
            IList<ChatMessage> messages,
            IList<ToolDefinition>? tools = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 使用标准OpenAI兼容格式
            var request = new
            {
                model = _config.ModelName,
                messages = messages,
                temperature = 0.7,
                max_tokens = 1000,
                stream = true
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
            {
                Content = requestContent
            };

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"API 调用失败: {ex.Message}\n响应内容: {errorContent}", ex);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("data: "))
                {
                    line = line.Substring(6);
                }

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
                }

                if (streamResponse != null)
                {
                    yield return streamResponse;
                }
            }
        }
    }
}
