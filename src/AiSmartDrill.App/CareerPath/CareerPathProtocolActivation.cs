using System.IO;
using System.Text;
using System.Text.Json;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 解析浏览器通过 <c>aismartdrill://launch?payload=...</c> 拉起应用时传入的参数，
/// 并将其中的 JSON 载荷落为临时 <c>.skillpkg</c> 文件，复用现有的导入链路。
/// </summary>
public static class CareerPathProtocolActivation
{
    /// <summary>
    /// 网页端与桌面端约定的自定义协议名。
    /// </summary>
    public const string ProtocolScheme = "aismartdrill";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// 尝试解析启动参数中的自定义协议。
    /// </summary>
    /// <param name="args">应用启动参数。</param>
    /// <param name="errorMessage">识别到协议但解析失败时的错误消息；未识别协议时为 null。</param>
    /// <returns>是否识别并处理了协议参数。</returns>
    public static bool TryApply(string[]? args, out string? errorMessage)
    {
        errorMessage = null;
        var raw = ExtractProtocolArgument(args);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            var uri = new Uri(raw, UriKind.Absolute);
            if (!uri.Scheme.Equals(ProtocolScheme, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(uri.Host, "launch", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "仅支持 aismartdrill://launch 协议入口。";
                return true;
            }

            var payloadBase64 = GetQueryValue(uri.Query, "payload");
            if (string.IsNullOrWhiteSpace(payloadBase64))
            {
                errorMessage = "协议链接缺少 payload 参数。";
                return true;
            }

            var json = DecodePayloadJson(payloadBase64);
            var package = JsonSerializer.Deserialize<CareerPathSkillPackage>(json, JsonOptions);
            if (package is null)
            {
                errorMessage = "协议载荷为空或无法识别。";
                return true;
            }

            if (package.Skills.Length == 0)
            {
                errorMessage = "协议载荷中没有可导入的技能点。";
                return true;
            }

            var tempPath = PersistProtocolPackage(json);
            CareerPathStartupState.ImportPath = tempPath;
            CareerPathStartupState.ModeFromCli = null;
            CareerPathStartupState.AutoProceedFromCli = ParseBoolQueryValue(uri.Query, "auto");
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = "协议载荷 JSON 无效：" + ex.Message;
            return true;
        }
        catch (FormatException ex)
        {
            errorMessage = "协议 payload 不是有效的 Base64Url：" + ex.Message;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = "处理网页拉起参数失败：" + ex.Message;
            return true;
        }
    }

    private static string? ExtractProtocolArgument(string[]? args)
    {
        if (args is null || args.Length == 0)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            if (arg.StartsWith(ProtocolScheme + "://", StringComparison.OrdinalIgnoreCase))
            {
                return arg.Trim();
            }
        }

        return null;
    }

    private static string DecodePayloadJson(string payloadBase64)
    {
        var normalized = payloadBase64
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        var bytes = Convert.FromBase64String(normalized);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string PersistProtocolPackage(string json)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AiSmartDrill",
            "careerpath-inbox");
        Directory.CreateDirectory(root);
        CleanupOldPackages(root);

        var fileName = $"careerpath-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.skillpkg";
        var path = Path.Combine(root, fileName);
        File.WriteAllText(path, json, new UTF8Encoding(false));
        return path;
    }

    private static void CleanupOldPackages(string root)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.skillpkg"))
            {
                var info = new FileInfo(file);
                if (info.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-7))
                {
                    info.Delete();
                }
            }
        }
        catch
        {
            // 临时目录清理失败不影响主流程。
        }
    }

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var trimmed = query.TrimStart('?');
        var segments = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var pair = segment.Split('=', 2);
            if (!pair[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
        }

        return null;
    }

    private static bool ParseBoolQueryValue(string query, string key)
    {
        var value = GetQueryValue(query, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
               || value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
