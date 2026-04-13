using System.Text.Json;

namespace AiSmartDrill.App.Drill.Ai.Ark;

/// <summary>
/// 反序列化火山方舟 <c>POST /chat/completions</c> 返回的 JSON（兼容 snake_case 字段名，与 <c>ChatCompletionResponse</c> 等类型上的 <c>JsonPropertyName</c> 配合使用）。
/// </summary>
public static class ArkHttpResponseJsonOptions
{
    /// <summary>
    /// HTTP 响应体反序列化选项。
    /// </summary>
    public static JsonSerializerOptions Value { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
}
