namespace AiSmartDrill.App.Drill.Ai.Ark;

/// <summary>
/// 将控制台或文档中复制的方舟 API 地址规范为 <see cref="System.Net.Http.HttpClient.BaseAddress"/> 可用的「仅到 <c>/api/v3/</c>」形式，
/// 避免用户把官方示例中的完整 <c>.../chat/completions</c> 填进配置后请求变成 <c>.../chat/completions/chat/completions</c>。
/// </summary>
public static class ArkApiEndpointNormalizer
{
    private const string ChatCompletionsPath = "/chat/completions";

    /// <summary>
    /// 若 <paramref name="baseUrl"/> 以 <c>/chat/completions</c> 结尾则去掉该段，再保证以 <c>/</c> 结尾，供相对路径 <c>chat/completions</c> 拼接。
    /// </summary>
    /// <param name="baseUrl">用户配置的基址；若为 <c>null</c> 或空白则返回空字符串（须在配置或界面中填写控制台给出的根地址）。</param>
    /// <returns>规范化后的基址，始终以 <c>/</c> 结尾；输入为空时返回空字符串。</returns>
    public static string ToChatCompletionsBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return string.Empty;

        var s = baseUrl.Trim();
        const StringComparison cmp = StringComparison.OrdinalIgnoreCase;
        while (s.EndsWith(ChatCompletionsPath, cmp))
            s = s[..^ChatCompletionsPath.Length].TrimEnd('/');

        return s.TrimEnd('/') + "/";
    }
}
