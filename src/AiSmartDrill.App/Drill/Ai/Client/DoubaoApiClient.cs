using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AiSmartDrill.App.Drill.Ai.Config;

namespace AiSmartDrill.App.Drill.Ai.Client
{
    public class DoubaoApiClient : IChatCompletionService
    {
        private readonly HttpClient _httpClient;
        private readonly DoubaoModelConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public DoubaoApiClient(HttpClient httpClient, DoubaoModelConfig config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _httpClient.BaseAddress = new Uri(config.BaseUrl);
            _httpClient.Timeout = config.Timeout;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ChatCompletionResponse> GenerateCompletionAsync(
            IList<ChatMessage> messages,
            IList<ToolDefinition>? tools = null,
            CancellationToken cancellationToken = default)
        {
            // 转换消息格式以匹配官方 API
            var inputMessages = new List<object>();
            foreach (var message in messages)
            {
                var content = new List<object>();
                content.Add(new { type = "input_text", text = message.Content });
                
                inputMessages.Add(new {
                    role = message.Role,
                    content = content
                });
            }

            var request = new
            {
                model = _config.ModelName,
                input = inputMessages,
                temperature = 0.7,
                max_tokens = 1000
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/responses", requestContent, cancellationToken);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"API 调用失败: {ex.Message}\n响应内容: {errorContent}", ex);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // 解析为Doubao的/responses端点响应格式
            var doubaoResponse = JsonSerializer.Deserialize<DoubaoResponse>(responseContent, _jsonOptions)!;
            
            // 转换为ChatCompletionResponse格式
            var chatResponse = new ChatCompletionResponse
            {
                Id = doubaoResponse.Id,
                Object = doubaoResponse.Object,
                Created = doubaoResponse.Created,
                Model = doubaoResponse.Model,
                Usage = doubaoResponse.Usage,
                Choices = new List<Choice>()
            };

            if (doubaoResponse.Output != null && doubaoResponse.Output.Count > 0)
            {
                // 提取第一个输出项的文本内容
                var firstOutput = doubaoResponse.Output[0];
                if (firstOutput.Content != null && firstOutput.Content.Count > 0)
                {
                    var textContent = string.Join("", firstOutput.Content
                        .Where(c => c.Type == "output_text" && c.Text != null)
                        .Select(c => c.Text));

                    chatResponse.Choices.Add(new Choice
                    {
                        Index = 0,
                        Message = new ChatMessage
                        {
                            Role = firstOutput.Role,
                            Content = textContent
                        },
                        FinishReason = "stop"
                    });
                }
            }

            return chatResponse;
        }

        public async IAsyncEnumerable<ChatCompletionStreamResponse> GenerateCompletionStreamAsync(
            IList<ChatMessage> messages,
            IList<ToolDefinition>? tools = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 转换消息格式以匹配官方 API
            var inputMessages = new List<object>();
            foreach (var message in messages)
            {
                var content = new List<object>();
                content.Add(new { type = "input_text", text = message.Content });
                
                inputMessages.Add(new {
                    role = message.Role,
                    content = content
                });
            }

            var request = new
            {
                model = _config.ModelName,
                input = inputMessages,
                temperature = 0.7,
                max_tokens = 1000,
                stream = true
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/responses")
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
