using System.Text.Json;

namespace AiSmartDrill.App.Drill.Ai.Ark;

/// <summary>
/// 从方舟/OpenAI 兼容错误响应体中提取可读说明（常见结构：<c>{"error":{"message":"...","code":"..."}}</c>）。
/// </summary>
public static class ArkApiErrorParser
{
    /// <summary>
    /// 将 HTTP 状态码与响应正文格式化为单行错误说明；无法解析时返回状态码与正文截断。
    /// </summary>
    public static string FormatHttpError(int statusCode, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return $"HTTP {statusCode}（无响应正文）";

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var err))
            {
                string? msg = null;
                string? code = null;
                if (err.ValueKind == JsonValueKind.Object)
                {
                    if (err.TryGetProperty("message", out var m))
                        msg = m.GetString();
                    if (err.TryGetProperty("code", out var c))
                        code = c.GetString();
                }
                else if (err.ValueKind == JsonValueKind.String)
                {
                    msg = err.GetString();
                }

                if (!string.IsNullOrEmpty(msg))
                {
                    return string.IsNullOrEmpty(code)
                        ? $"HTTP {statusCode}: {msg}"
                        : $"HTTP {statusCode} [{code}] {msg}";
                }
            }
        }
        catch (JsonException)
        {
            // 非 JSON 响应（如 HTML 网关页）
        }

        var trimmed = body.Length > 800 ? body[..800] + "…" : body;
        return $"HTTP {statusCode}: {trimmed}";
    }
}
