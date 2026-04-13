using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiSmartDrill.App.Drill.Ai.Ark;

/// <summary>
/// 方舟 / OpenAI 兼容接口中 <c>message.content</c> 可能为字符串，也可能为多段结构（数组）；统一读出为纯文本供业务使用。
/// </summary>
public sealed class ArkChatContentStringConverter : JsonConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadAsPlainText(ref reader);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ?? string.Empty);
    }

    private static string ReadAsPlainText(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString() ?? string.Empty;
            case JsonTokenType.Null:
                return string.Empty;
            case JsonTokenType.StartArray:
                using (var doc = JsonDocument.ParseValue(ref reader))
                    return ExtractFromElement(doc.RootElement);
            case JsonTokenType.StartObject:
                using (var doc = JsonDocument.ParseValue(ref reader))
                    return ExtractFromElement(doc.RootElement);
            default:
                using (var doc = JsonDocument.ParseValue(ref reader))
                    return ExtractFromElement(doc.RootElement);
        }
    }

    private static string ExtractFromElement(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return el.GetString() ?? string.Empty;
            case JsonValueKind.Array:
                var sb = new StringBuilder();
                foreach (var item in el.EnumerateArray())
                {
                    var part = ExtractFromElement(item);
                    if (part.Length > 0)
                        sb.Append(part);
                }

                return sb.ToString();
            case JsonValueKind.Object:
                if (el.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    return text.GetString() ?? string.Empty;
                if (el.TryGetProperty("content", out var content))
                    return ExtractFromElement(content);
                return string.Empty;
            default:
                return el.ToString();
        }
    }
}
