using Microsoft.Extensions.Options;

namespace AiSmartDrill.App.Drill.Ai.Config;

/// <summary>
/// 豆包模型配置管理类
/// </summary>
public class DoubaoModelConfig
{
    private readonly DoubaoModelOptions _options;

    /// <summary>
    /// 初始化 <see cref="DoubaoModelConfig"/> 的新实例
    /// </summary>
    /// <param name="options">配置选项</param>
    public DoubaoModelConfig(IOptions<DoubaoModelOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey => _options.ApiKey;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName => _options.ModelName;

    /// <summary>
    /// 基础 URL
    /// </summary>
    public string BaseUrl => _options.BaseUrl;

    /// <summary>
    /// 聊天完成 API 端点
    /// </summary>
    public string ChatCompletionsEndpoint => $"{BaseUrl}/chat/completions";

    /// <summary>
    /// 超时设置
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromSeconds(_options.TimeoutSeconds);

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries => _options.MaxRetries;

    /// <summary>
    /// 是否启用思考能力
    /// </summary>
    public bool EnableThinking => _options.EnableThinking;

    /// <summary>
    /// 是否启用视觉能力
    /// </summary>
    public bool EnableVision => _options.EnableVision;

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ApiKey) &&
               !string.IsNullOrEmpty(ModelName) &&
               !string.IsNullOrEmpty(BaseUrl);
    }
}
