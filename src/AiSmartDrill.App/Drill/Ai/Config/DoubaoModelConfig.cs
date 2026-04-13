using AiSmartDrill.App.Drill.Ai.Ark;
using Microsoft.Extensions.Options;

namespace AiSmartDrill.App.Drill.Ai.Config;

/// <summary>
/// 豆包模型配置管理类：从 <see cref="DoubaoModelOptions"/> 提供 Ark 调用所需的密钥、接入点与基址。
/// </summary>
public class DoubaoModelConfig
{
    private readonly DoubaoModelOptions _options;

    /// <summary>
    /// 初始化 <see cref="DoubaoModelConfig"/> 的新实例。
    /// </summary>
    /// <param name="optionsAccessor">由 DI 绑定的 <see cref="DoubaoModelOptions"/>（对应 appsettings 中 <c>DoubaoModel</c> 节）。</param>
    public DoubaoModelConfig(IOptions<DoubaoModelOptions> optionsAccessor)
    {
        _options = optionsAccessor.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
    }

    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey => _options.ApiKey;

    /// <summary>
    /// 请求体 <c>model</c> 字段：接入点 ID（<c>ep-...</c>）或模型 ID，与控制台配置一致即可。
    /// </summary>
    public string ModelName => _options.ModelName;

    /// <summary>
    /// 可选备忘（如控制台模型名称），不参与 HTTP 请求体。
    /// </summary>
    public string ModelId => _options.ModelId;

    /// <summary>
    /// 基础 URL
    /// </summary>
    public string BaseUrl => _options.BaseUrl;

    /// <summary>
    /// 聊天完成 API 完整 URL（与官方文档中的 <c>POST .../chat/completions</c> 一致）。
    /// </summary>
    public string ChatCompletionsEndpoint =>
        $"{ArkApiEndpointNormalizer.ToChatCompletionsBaseUrl(BaseUrl).TrimEnd('/')}/chat/completions";

    /// <summary>
    /// 采样温度。
    /// </summary>
    public double Temperature => _options.Temperature;

    /// <summary>
    /// 单次回复最大 token 数。
    /// </summary>
    public int MaxTokens => _options.MaxTokens;

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
