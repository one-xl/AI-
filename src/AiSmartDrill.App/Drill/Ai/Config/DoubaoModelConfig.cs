using AiSmartDrill.App.Drill.Ai.Ark;
using AiSmartDrill.App;
using Microsoft.Extensions.Options;

namespace AiSmartDrill.App.Drill.Ai.Config;

/// <summary>
/// 豆包模型运行时配置：支持多档案（端点/密钥/接入点）与界面切换当前档案。
/// </summary>
public sealed class DoubaoModelConfig
{
    private readonly DoubaoModelOptions _options;
    private readonly object _sync = new();
    private string _activeProfileId;

    /// <summary>
    /// 初始化：应用 <see cref="AiModelProfilePreferenceStore"/> 与 <see cref="DoubaoModelOptions.ActiveProfileId"/>。
    /// </summary>
    public DoubaoModelConfig(IOptions<DoubaoModelOptions> optionsAccessor)
    {
        _options = optionsAccessor.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        var profiles = _options.Profiles ?? new Dictionary<string, DoubaoModelProfileOptions>(StringComparer.OrdinalIgnoreCase);
        if (profiles.Count == 0)
            throw new InvalidOperationException("DoubaoModel:Profiles 为空；请检查配置绑定。");

        var saved = AiModelProfilePreferenceStore.LoadActiveProfileIdOrNull();
        if (!string.IsNullOrWhiteSpace(saved) && TryCanonicalProfileId(saved, out var canonSaved))
            _activeProfileId = canonSaved;
        else if (!string.IsNullOrWhiteSpace(_options.ActiveProfileId) &&
                 TryCanonicalProfileId(_options.ActiveProfileId, out var canonOpt))
            _activeProfileId = canonOpt;
        else
            _activeProfileId = profiles.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
    }

    /// <summary>
    /// 当前生效的档案键（与 <c>Profiles</c> 中键一致）。
    /// </summary>
    public string ActiveProfileId
    {
        get
        {
            lock (_sync)
                return _activeProfileId;
        }
    }

    /// <summary>
    /// 列出可供界面绑定的档案项。
    /// </summary>
    public IReadOnlyList<DoubaoModelProfileListItem> ListProfileItems()
    {
        lock (_sync)
        {
            return _options.Profiles!
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new DoubaoModelProfileListItem(kv.Key, FormatLabel(kv.Key, kv.Value)))
                .ToList();
        }
    }

    /// <summary>
    /// 切换当前档案并持久化偏好。
    /// </summary>
    public void SetActiveProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("档案 Id 不能为空。", nameof(profileId));

        lock (_sync)
        {
            if (!TryCanonicalProfileId(profileId, out var canon))
                throw new ArgumentException($"未知模型档案：{profileId}", nameof(profileId));

            if (string.Equals(_activeProfileId, canon, StringComparison.Ordinal))
                return;

            _activeProfileId = canon;
        }

        AiModelProfilePreferenceStore.SaveActiveProfileId(_activeProfileId);
    }

    /// <summary>
    /// 供 HTTP 客户端在每次请求前读取的合并快照。
    /// </summary>
    public DoubaoConnectionSnapshot GetConnectionSnapshot()
    {
        lock (_sync)
        {
            var prof = _options.Profiles![_activeProfileId];
            return DoubaoConnectionSnapshot.Merge(_options, prof);
        }
    }

    /// <summary>
    /// API 密钥（当前档案）。
    /// </summary>
    public string ApiKey => GetConnectionSnapshot().ApiKey;

    /// <summary>
    /// 请求体 model 字段（当前档案）。
    /// </summary>
    public string ModelName => GetConnectionSnapshot().ModelName;

    /// <summary>
    /// 备忘模型 Id（当前档案）。
    /// </summary>
    public string ModelId => GetConnectionSnapshot().ModelId;

    /// <summary>
    /// 根路径 URL（当前档案）。
    /// </summary>
    public string BaseUrl => GetConnectionSnapshot().BaseUrl;

    /// <summary>
    /// Chat Completions 完整 URL。
    /// </summary>
    public string ChatCompletionsEndpoint =>
        string.IsNullOrWhiteSpace(BaseUrl)
            ? string.Empty
            : $"{ArkApiEndpointNormalizer.ToChatCompletionsBaseUrl(BaseUrl).TrimEnd('/')}/chat/completions";

    /// <summary>
    /// 采样温度。
    /// </summary>
    public double Temperature => GetConnectionSnapshot().Temperature;

    /// <summary>
    /// 单次回复最大 token。
    /// </summary>
    public int MaxTokens => GetConnectionSnapshot().MaxTokens;

    /// <summary>
    /// 超时。
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromSeconds(GetConnectionSnapshot().TimeoutSeconds);

    /// <summary>
    /// 最大重试次数。
    /// </summary>
    public int MaxRetries => GetConnectionSnapshot().MaxRetries;

    /// <summary>
    /// 是否启用思考能力。
    /// </summary>
    public bool EnableThinking => GetConnectionSnapshot().EnableThinking;

    /// <summary>
    /// 是否启用视觉能力。
    /// </summary>
    public bool EnableVision => GetConnectionSnapshot().EnableVision;

    /// <summary>
    /// 当前档案连接信息是否可用于发起请求。
    /// </summary>
    public bool IsValid()
    {
        var s = GetConnectionSnapshot();
        return !string.IsNullOrWhiteSpace(s.ApiKey) &&
               !string.IsNullOrWhiteSpace(s.ModelName) &&
               !string.IsNullOrWhiteSpace(s.BaseUrl);
    }

    /// <summary>
    /// 运行时追加一个模型档案（已写入 appsettings 后调用）。
    /// </summary>
    public DoubaoModelProfileListItem AddProfileAtRuntime(string id, DoubaoModelProfileOptions profile)
    {
        lock (_sync)
        {
            _options.Profiles![id] = profile;
        }

        return new DoubaoModelProfileListItem(id, FormatLabel(id, profile));
    }

    private bool TryCanonicalProfileId(string requested, out string canonical)
    {
        canonical = string.Empty;
        foreach (var k in _options.Profiles!.Keys)
        {
            if (k.Equals(requested.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                canonical = k;
                return true;
            }
        }

        return false;
    }

    private static string FormatLabel(string id, DoubaoModelProfileOptions p)
    {
        var name = p.DisplayName?.Trim();
        return string.IsNullOrEmpty(name) ? id : $"{name}（{id}）";
    }
}
