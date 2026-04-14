using System.IO;
using System.Text.Json;

namespace AiSmartDrill.App;

/// <summary>
/// 表示一条可序列化的主题偏好记录。
/// </summary>
public readonly record struct ThemePreferenceState(bool IsDark, bool UseSunAutoTheme);

/// <summary>
/// 将浅色/深色偏好及「是否按日出日落自动切换」持久化到本地应用数据目录。
/// </summary>
public static class ThemePreferenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    /// <summary>
    /// 读取主题偏好；文件缺失时返回深色 + 默认开启日出日落自动。
    /// </summary>
    public static ThemePreferenceState LoadPreferencesOrDefault()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
            {
                return new ThemePreferenceState(true, true);
            }

            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<ThemePreferenceDto>(json, JsonOptions);
            if (dto is null)
            {
                return new ThemePreferenceState(true, true);
            }

            var useSun = dto.UseSunAutoTheme ?? true;
            return new ThemePreferenceState(dto.IsDark, useSun);
        }
        catch
        {
            return new ThemePreferenceState(true, true);
        }
    }

    /// <summary>
    /// 兼容旧版仅含 <c>IsDark</c> 的读取方式。
    /// </summary>
    public static bool LoadIsDarkOrDefault(bool defaultIsDark = true) =>
        LoadPreferencesOrDefault().IsDark;

    /// <summary>
    /// 保存主题状态（手动与自动标志一并写入）。
    /// </summary>
    public static void SavePreferences(ThemePreferenceState state)
    {
        try
        {
            var dir = Path.GetDirectoryName(GetPath());
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var dto = new ThemePreferenceDto { IsDark = state.IsDark, UseSunAutoTheme = state.UseSunAutoTheme };
            File.WriteAllText(GetPath(), JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // 忽略磁盘错误，不影响应用使用。
        }
    }

    /// <summary>
    /// 仅更新深色标志（保留当前 UseSunAutoTheme）。
    /// </summary>
    public static void SaveIsDark(bool isDark)
    {
        var cur = LoadPreferencesOrDefault();
        SavePreferences(new ThemePreferenceState(isDark, cur.UseSunAutoTheme));
    }

    private static string GetPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "AiSmartDrill", "theme.json");
    }

    private sealed class ThemePreferenceDto
    {
        public bool IsDark { get; set; }

        /// <summary>
        /// 为 null 表示旧文件未包含该字段，按「默认开启日出日落自动」处理。
        /// </summary>
        public bool? UseSunAutoTheme { get; set; }
    }
}
