using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 解析 CLI <c>--mode</c> 与 JSON <c>practice_mode</c>，并以 JSON 为准在不一致时记警告日志。
/// </summary>
public static class CareerPathModeResolver
{
    /// <summary>
    /// 将 JSON 中的 <paramref name="practiceModeRaw"/> 解析为枚举；无法识别时默认为 <see cref="CareerPathPracticeModeKind.Direct"/>。
    /// </summary>
    public static CareerPathPracticeModeKind ParseFromPackage(string? practiceModeRaw)
    {
        var s = (practiceModeRaw ?? string.Empty).Trim().ToLowerInvariant();
        return s switch
        {
            "ai_recommend" or "ai-recommend" or "airecommend" => CareerPathPracticeModeKind.AiRecommend,
            _ => CareerPathPracticeModeKind.Direct
        };
    }

    /// <summary>
    /// 解析命令行 <c>--mode</c> 参数（可为 <c>ai-recommend</c>）。
    /// </summary>
    public static CareerPathPracticeModeKind? ParseFromCli(string? cliModeRaw)
    {
        if (string.IsNullOrWhiteSpace(cliModeRaw))
        {
            return null;
        }

        var s = cliModeRaw.Trim().ToLowerInvariant();
        return s switch
        {
            "direct" => CareerPathPracticeModeKind.Direct,
            "ai-recommend" or "ai_recommend" or "airecommend" => CareerPathPracticeModeKind.AiRecommend,
            _ => null
        };
    }

    /// <summary>
    /// 以技能包为准得到最终模式；若 CLI 与 JSON 不一致则写警告日志。
    /// </summary>
    public static CareerPathPracticeModeKind ResolveEffective(
        CareerPathSkillPackage package,
        string? cliModeRaw,
        ILogger logger)
    {
        var fromPackage = ParseFromPackage(package.PracticeMode);
        var fromCli = ParseFromCli(cliModeRaw);
        if (fromCli is { } c && c != fromPackage)
        {
            logger.LogWarning(
                "命令行 --mode ({Cli}) 与技能包 practice_mode ({Json}) 不一致，已以技能包为准。",
                cliModeRaw,
                package.PracticeMode);
        }

        return fromPackage;
    }
}
