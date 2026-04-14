using System.IO;
using System.Text.Json;
using AiSmartDrill.App.Drill.Ai.Config;

namespace AiSmartDrill.App;

/// <summary>
/// 将界面「增加模型」写入本地应用数据目录，避免每次生成覆盖 <c>bin</c> 下 <c>appsettings.json</c> 导致档案丢失；启动时合并进 <see cref="DoubaoModelOptions.Profiles"/>。
/// </summary>
public static class UserDoubaoProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// 把磁盘上的用户档案合并进已规范化的 <paramref name="options"/>（同键则覆盖）。
    /// </summary>
    public static void MergeInto(DoubaoModelOptions options)
    {
        var path = GetPath();
        if (!File.Exists(path))
            return;

        try
        {
            var json = File.ReadAllText(path);
            var map = JsonSerializer.Deserialize<Dictionary<string, DoubaoModelProfileOptions>>(json, JsonOptions);
            if (map is null || map.Count == 0)
                return;

            foreach (var kv in map)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;

                options.Profiles![kv.Key.Trim()] = kv.Value ?? new DoubaoModelProfileOptions();
            }
        }
        catch
        {
            // 损坏时忽略，保留 appsettings 内档案
        }
    }

    /// <summary>
    /// 新增或覆盖一条用户档案并写回磁盘。
    /// </summary>
    public static void Upsert(string profileId, DoubaoModelProfileOptions profile)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var map = new Dictionary<string, DoubaoModelProfileOptions>(comparer);
        var path = GetPath();
        if (File.Exists(path))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<Dictionary<string, DoubaoModelProfileOptions>>(
                    File.ReadAllText(path), JsonOptions);
                if (existing is not null)
                {
                    foreach (var kv in existing)
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Key))
                            map[kv.Key.Trim()] = kv.Value ?? new DoubaoModelProfileOptions();
                    }
                }
            }
            catch
            {
                // 覆盖损坏文件
            }
        }

        map[profileId.Trim()] = profile;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, JsonSerializer.Serialize(map, JsonOptions));
    }

    private static string GetPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "AiSmartDrill", "doubao-user-profiles.json");
    }
}
