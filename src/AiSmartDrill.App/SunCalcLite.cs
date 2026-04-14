namespace AiSmartDrill.App;

/// <summary>
/// 基于 <see href="https://github.com/mourner/suncalc">suncalc</see>（BSD）算法的精简 C# 移植，
/// 仅计算官方日出/日落（太阳高度角 -0.833°），输出本地时间。
/// </summary>
public static class SunCalcLite
{
    private const double PI = Math.PI;
    private const double Rad = PI / 180.0;
    private const double DayMs = 86400000.0;
    private const double J1970 = 2440588.0;
    private const double J2000 = 2451545.0;
    private const double E = Rad * 23.4397;
    private const double J0 = 0.0009;

    /// <summary>
    /// 计算指定公历日（按本机时区日历日）在观测点的日出、日落本地时间。
    /// </summary>
    /// <param name="localCalendarDate">任意落在该日内的本地时间（通常取 DateTime.Today）。</param>
    /// <param name="latitudeDeg">北纬为正、南纬为负。</param>
    /// <param name="longitudeDeg">东经为正、西经为负。</param>
    /// <param name="sunriseLocal">日出本地时间。</param>
    /// <param name="sunsetLocal">日落本地时间。</param>
    /// <returns>若纬度极区导致无法计算则返回 false。</returns>
    public static bool TryGetSunriseSunsetLocal(
        DateTime localCalendarDate,
        double latitudeDeg,
        double longitudeDeg,
        out DateTime sunriseLocal,
        out DateTime sunsetLocal)
    {
        sunriseLocal = default;
        sunsetLocal = default;

        if (latitudeDeg is < -90 or > 90 || longitudeDeg is < -180 or > 180)
        {
            return false;
        }

        var noonLocal = DateTime.SpecifyKind(localCalendarDate.Date.AddHours(12), DateTimeKind.Local);
        var utc = noonLocal.Kind == DateTimeKind.Local ? noonLocal.ToUniversalTime() : TimeZoneInfo.ConvertTimeToUtc(noonLocal, TimeZoneInfo.Local);

        var lw = Rad * -longitudeDeg;
        var phi = Rad * latitudeDeg;
        const double heightMeters = 0;
        var dh = ObserverAngle(heightMeters);
        var d = ToDays(utc);
        var n = Math.Round(d - J0 - lw / (2 * PI));
        var ds = ApproxTransit(0, lw, n);
        var m = SolarMeanAnomaly(ds);
        var l = EclipticLongitude(m);
        var dec = Declination(l, 0);
        var jNoon = SolarTransitJ(ds, m, l);

        const double sunAngleDeg = -0.833;
        var h0 = (sunAngleDeg + dh) * Rad;
        double jSet;
        try
        {
            jSet = GetSetJ(h0, lw, phi, dec, n, m, l);
        }
        catch
        {
            return false;
        }

        var jRise = jNoon - (jSet - jNoon);

        var riseUtc = FromJulian(jRise);
        var setUtc = FromJulian(jSet);
        if (riseUtc >= setUtc)
        {
            return false;
        }

        sunriseLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(riseUtc, DateTimeKind.Utc), TimeZoneInfo.Local);
        sunsetLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(setUtc, DateTimeKind.Utc), TimeZoneInfo.Local);
        return true;
    }

    private static double ObserverAngle(double heightMeters) => -2.076 * Math.Sqrt(Math.Max(0, heightMeters)) / 60.0;

    private static double ToJulian(DateTime utc) =>
        (utc - DateTime.UnixEpoch).TotalMilliseconds / DayMs - 0.5 + J1970;

    private static DateTime FromJulian(double j) =>
        DateTime.UnixEpoch.AddMilliseconds((j + 0.5 - J1970) * DayMs);

    private static double ToDays(DateTime utc) => ToJulian(utc) - J2000;

    private static double RightAscension(double l, double b) =>
        Math.Atan2(Math.Sin(l) * Math.Cos(E) - Math.Tan(b) * Math.Sin(E), Math.Cos(l));

    private static double Declination(double l, double b) =>
        Math.Asin(Math.Sin(b) * Math.Cos(E) + Math.Cos(b) * Math.Sin(E) * Math.Sin(l));

    private static double SolarMeanAnomaly(double d) => Rad * (357.5291 + 0.98560028 * d);

    private static double EclipticLongitude(double m)
    {
        var c = Rad * (1.9148 * Math.Sin(m) + 0.02 * Math.Sin(2 * m) + 0.0003 * Math.Sin(3 * m));
        const double p = Rad * 102.9372;
        return m + c + p + PI;
    }

    private static double ApproxTransit(double ht, double lw, double n) => J0 + (ht + lw) / (2 * PI) + n;

    private static double SolarTransitJ(double ds, double m, double l) => J2000 + ds + 0.0053 * Math.Sin(m) - 0.0069 * Math.Sin(2 * l);

    private static double HourAngle(double h, double phi, double d)
    {
        var cosArg = (Math.Sin(h) - Math.Sin(phi) * Math.Sin(d)) / (Math.Cos(phi) * Math.Cos(d));
        if (double.IsNaN(cosArg) || double.IsInfinity(cosArg))
        {
            throw new InvalidOperationException("hour angle");
        }

        cosArg = Math.Clamp(cosArg, -1.0, 1.0);
        return Math.Acos(cosArg);
    }

    private static double GetSetJ(double h, double lw, double phi, double dec, double n, double m, double l)
    {
        var w = HourAngle(h, phi, dec);
        var a = ApproxTransit(w, lw, n);
        return SolarTransitJ(a, m, l);
    }
}
