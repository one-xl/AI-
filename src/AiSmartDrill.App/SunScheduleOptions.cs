namespace AiSmartDrill.App;

/// <summary>
/// 日出/日落自动主题与观测点配置（绑定 appsettings.json 中 SunSchedule 节）。
/// </summary>
public sealed class SunScheduleOptions
{
    /// <summary>
    /// 获取或设置是否启用按日出日落自动切换主题。
    /// </summary>
    public bool EnableAutoTheme { get; set; } = true;

    /// <summary>
    /// 获取或设置观测点纬度（北纬为正），默认北京附近。
    /// </summary>
    public double Latitude { get; set; } = 39.9042;

    /// <summary>
    /// 获取或设置观测点经度（东经为正），默认北京附近。
    /// </summary>
    public double Longitude { get; set; } = 116.4074;
}
