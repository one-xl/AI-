using System.Windows;

namespace AiSmartDrill.App;

/// <summary>
/// 在应用资源中合并浅色或深色主题字典，供 <see cref="DynamicResource"/> 解析。
/// </summary>
public static class AppTheme
{
    private static ResourceDictionary? _themeDictionary;

    /// <summary>
    /// 当前是否为深色主题（与最后一次 <see cref="Apply"/> 一致）。
    /// </summary>
    public static bool IsDark { get; private set; } = true;

    /// <summary>
    /// 从用户偏好加载并应用主题（不读取日出日落配置）；设计器或测试用。
    /// 正常启动请使用 <see cref="DayNightThemeBootstrap.ApplyStartupTheme"/>。
    /// </summary>
    public static void InitializeFromStore()
    {
        var prefs = ThemePreferenceStore.LoadPreferencesOrDefault();
        Apply(prefs.IsDark);
    }

    /// <summary>
    /// 切换合并的主题资源字典。
    /// </summary>
    /// <param name="isDark">true 为深色，false 为浅色。</param>
    public static void Apply(bool isDark)
    {
        IsDark = isDark;
        var app = Application.Current;
        var pack = isDark
            ? "pack://application:,,,/Themes/Theme.Dark.xaml"
            : "pack://application:,,,/Themes/Theme.Light.xaml";
        var newDict = new ResourceDictionary { Source = new Uri(pack, UriKind.Absolute) };

        if (_themeDictionary is not null)
        {
            app.Resources.MergedDictionaries.Remove(_themeDictionary);
        }

        _themeDictionary = newDict;
        app.Resources.MergedDictionaries.Insert(0, _themeDictionary);
    }
}
