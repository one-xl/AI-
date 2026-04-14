namespace AiSmartDrill.App.Drill.Ai.Config;

/// <summary>
/// <c>appsettings.json</c> 中 <c>DoubaoModel:Profiles</c> 下某一命名的连接配置；未填字段继承根节同名项。
/// </summary>
public sealed class DoubaoModelProfileOptions
{
    /// <summary>
    /// 界面下拉展示名；为空则用配置键（如 <c>default</c>）。
    /// </summary>
    public string? DisplayName { get; set; }

    public string? ApiKey { get; set; }

    public string? ModelName { get; set; }

    public string? ModelId { get; set; }

    public string? BaseUrl { get; set; }

    public double? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public int? TimeoutSeconds { get; set; }

    public int? MaxRetries { get; set; }

    public bool? EnableThinking { get; set; }

    public bool? EnableVision { get; set; }
}
