namespace AiSmartDrill.App.Drill.Ai.Config;

/// <summary>
/// 根配置与当前档案合并后的单次连接参数（供 HTTP 客户端按请求使用）。
/// </summary>
public readonly record struct DoubaoConnectionSnapshot(
    string ApiKey,
    string ModelName,
    string ModelId,
    string BaseUrl,
    double Temperature,
    int MaxTokens,
    int TimeoutSeconds,
    int MaxRetries,
    bool EnableThinking,
    bool EnableVision)
{
    /// <summary>
    /// 将档案字段与根节 <see cref="DoubaoModelOptions"/> 合并；档案中非空项覆盖根节。
    /// </summary>
    public static DoubaoConnectionSnapshot Merge(DoubaoModelOptions root, DoubaoModelProfileOptions profile)
    {
        static string Pick(string? fromProfile, string fromRoot) =>
            string.IsNullOrWhiteSpace(fromProfile) ? fromRoot : fromProfile.Trim();

        return new DoubaoConnectionSnapshot(
            Pick(profile.ApiKey, root.ApiKey),
            Pick(profile.ModelName, root.ModelName),
            Pick(profile.ModelId, root.ModelId),
            Pick(profile.BaseUrl, root.BaseUrl),
            profile.Temperature ?? root.Temperature,
            profile.MaxTokens ?? root.MaxTokens,
            profile.TimeoutSeconds ?? root.TimeoutSeconds,
            profile.MaxRetries ?? root.MaxRetries,
            profile.EnableThinking ?? root.EnableThinking,
            profile.EnableVision ?? root.EnableVision);
    }
}
