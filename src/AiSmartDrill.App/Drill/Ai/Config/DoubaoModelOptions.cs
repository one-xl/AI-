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
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = "doubao-seed-1.8";

    /// <summary>
    /// 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://ark.cn-beijing.volces.com/api/v3";

    /// <summary>
    /// 超时设置（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 是否启用思考能力
    /// </summary>
    public bool EnableThinking { get; set; } = true;

    /// <summary>
    /// 是否启用视觉能力
    /// </summary>
    public bool EnableVision { get; set; } = true;
}
