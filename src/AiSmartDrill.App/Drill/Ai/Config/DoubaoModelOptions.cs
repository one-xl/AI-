using System;

namespace AiSmartDrill.App.Drill.Ai.Config;

/// <summary>
/// 豆包模型配置选项，用于依赖注入
/// </summary>
public class DoubaoModelOptions
{
    /// <summary>
    /// 配置部分名称
    /// </summary>
    public const string SectionName = "DoubaoModel";

    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 请求体 <c>model</c> 字段（二选一，勿与 <see cref="ModelId"/> 同时用于同一语义）：填「推理接入点」ID（<c>ep-...</c>，推荐）；或直接填模型 ID（如 <c>doubao-seed-1-8-251228</c>）。
    /// </summary>
    public string ModelName { get; set; } = "ep-m-20260404151221-gbtfh";

    /// <summary>
    /// 可选：控制台模型 ID 备忘（仅日志/对照，不参与请求体）。使用接入点调用时可留空；使用模型 ID 调用时通常也不必填此项。
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// 方舟 OpenAI 兼容根路径（到 <c>/api/v3/</c> 即可）；若误粘贴官方示例中的完整 <c>.../chat/completions</c>，客户端会自动截断。
    /// </summary>
    public string BaseUrl { get; set; } = "https://ark.cn-beijing.volces.com/api/v3/";

    /// <summary>
    /// 采样温度（方舟 <c>chat/completions</c> 的 <c>temperature</c>）。
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// 单次回复最大 token 数（<c>max_tokens</c>），错题批量/长 JSON 时可适当增大。
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// 超时设置（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 是否启用思考能力（若服务端/模型支持，由后续请求参数扩展使用；与「仅文本」不冲突）。
    /// </summary>
    public bool EnableThinking { get; set; } = true;

    /// <summary>
    /// 是否启用视觉/多模态。当前 <see cref="Client.ArkChatCompletionClient"/> 仅调用文本 Chat Completions，应置为 <c>false</c>。
    /// </summary>
    public bool EnableVision { get; set; } = false;
}
