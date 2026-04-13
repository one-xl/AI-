using System.Text.Json;

namespace AiSmartDrill.App.Drill.Ai.Ark;

/// <summary>
/// 解析模型返回的业务 JSON（推荐理由、计划、错题分析等）时使用的序列化选项：属性名大小写不敏感。
/// </summary>
public static class ArkChatJsonDefaults
{
    /// <summary>
    /// 用于反序列化模型输出的 DTO（非 Ark HTTP 原始响应体）。
    /// </summary>
    public static JsonSerializerOptions ModelPayloadOptions { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}
