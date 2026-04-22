using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 将 <c>aismartdrill://</c> 注册到当前 Windows 用户，
/// 使浏览器点击网页按钮时能够回调到桌面程序。
/// </summary>
public static class CareerPathProtocolRegistration
{
    /// <summary>
    /// 注册表中协议根键的相对路径。
    /// </summary>
    public const string RegistryPath = @"Software\Classes\" + CareerPathProtocolActivation.ProtocolScheme;

    /// <summary>
    /// 确保当前用户已注册协议处理器。
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    public static void EnsureRegistered(ILogger logger)
    {
        try
        {
            var executablePath = ResolveExecutablePath();
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                logger.LogWarning("无法注册 aismartdrill 协议：未找到当前可执行文件路径。");
                return;
            }

            using var root = Registry.CurrentUser.CreateSubKey(RegistryPath);
            if (root is null)
            {
                logger.LogWarning("无法注册 aismartdrill 协议：注册表根键创建失败。");
                return;
            }

            root.SetValue(string.Empty, "URL:AiSmartDrill Protocol");
            root.SetValue("URL Protocol", string.Empty);

            using var iconKey = root.CreateSubKey("DefaultIcon");
            iconKey?.SetValue(string.Empty, $"\"{executablePath}\",0");

            using var commandKey = root.CreateSubKey(@"shell\open\command");
            commandKey?.SetValue(string.Empty, $"\"{executablePath}\" \"%1\"");

            logger.LogInformation("已确保注册 aismartdrill 协议，目标可执行文件：{Path}", executablePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "注册 aismartdrill 协议失败");
        }
    }

    private static string? ResolveExecutablePath()
    {
        var currentProcessPath = Process.GetCurrentProcess().MainModule?.FileName
                                 ?? Environment.ProcessPath
                                 ?? System.Reflection.Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(currentProcessPath) &&
            currentProcessPath.Contains(Path.DirectorySeparatorChar + "publish" + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            return currentProcessPath;
        }

        var publishPath = TryResolvePublishedExecutableFromAppBase();
        return !string.IsNullOrWhiteSpace(publishPath) ? publishPath : currentProcessPath;
    }

    private static string? TryResolvePublishedExecutableFromAppBase()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var current = new DirectoryInfo(baseDir);
            while (current is not null)
            {
                var candidate = Path.Combine(
                    current.FullName,
                    "publish",
                    "AiSmartDrill-win-x64",
                    "AiSmartDrill.App.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }
        catch
        {
            // 忽略路径探测失败，回退到当前进程路径。
        }

        return null;
    }
}
