using Microsoft.Extensions.Configuration;

// Get<T>() 扩展位于 Microsoft.Extensions.Configuration.Binder（项目已引用）。
namespace AiSmartDrill.App;

/// <summary>
/// 应用启动时根据配置与用户偏好，在「日出后浅色 / 日落后深色」与纯手动主题之间择一应用。
/// </summary>
public static class DayNightThemeBootstrap
{
    /// <summary>
    /// 在构建 <see cref="IConfiguration"/> 之后调用：若启用日出日落策略且用户未关闭，则按本机时刻与经纬度计算主题。
    /// </summary>
    /// <param name="configuration">应用配置。</param>
    public static void ApplyStartupTheme(IConfiguration configuration)
    {
        var sun = configuration.GetSection("SunSchedule").Get<SunScheduleOptions>() ?? new SunScheduleOptions();
        var prefs = ThemePreferenceStore.LoadPreferencesOrDefault();

        if (sun.EnableAutoTheme && prefs.UseSunAutoTheme &&
            SunCalcLite.TryGetSunriseSunsetLocal(DateTime.Today, sun.Latitude, sun.Longitude, out var rise, out var set))
        {
            AppTheme.Apply(IsNight(DateTime.Now, rise, set));
        }
        else
        {
            AppTheme.Apply(prefs.IsDark);
        }
    }

    /// <summary>
    /// 判断本地时刻是否处于「夜晚」区间：当日日出之前或日落及之后。
    /// </summary>
    public static bool IsNight(DateTime nowLocal, DateTime sunriseLocal, DateTime sunsetLocal) =>
        nowLocal < sunriseLocal || nowLocal >= sunsetLocal;
}
