using System.IO;
using System.Text.Json;

namespace AiSmartDrill.App;

/// <summary>
/// 将用户选择的方舟模型档案 Id 持久化到本地应用数据目录。
/// </summary>
public static class AiModelProfilePreferenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    /// <summary>
    /// 读取上次选择的档案 Id；无效或缺失时返回 <c>null</c>。
    /// </summary>
    public static string? LoadActiveProfileIdOrNull()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<Dto>(json, JsonOptions);
            return string.IsNullOrWhiteSpace(dto?.ActiveProfileId) ? null : dto.ActiveProfileId.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 保存当前选择的档案 Id。
    /// </summary>
    public static void SaveActiveProfileId(string profileId)
    {
        try
        {
            var dir = Path.GetDirectoryName(GetPath());
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var dto = new Dto { ActiveProfileId = profileId };
            File.WriteAllText(GetPath(), JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch
        {
            // 忽略磁盘错误
        }
    }

    private static string GetPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "AiSmartDrill", "ai-model-profile.json");
    }

    private sealed class Dto
    {
        public string? ActiveProfileId { get; set; }
    }
}
