using System.IO;
using System.Text;
using System.Text.Json;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 从磁盘加载 <see cref="CareerPathSkillPackage"/>（UTF-8 JSON）。
/// </summary>
public static class CareerPathSkillPackageJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// 尝试读取并反序列化技能包文件。
    /// </summary>
    /// <param name="absolutePath">绝对路径（扩展名可为 .skillpkg）。</param>
    /// <param name="package">成功时输出反序列化结果。</param>
    /// <param name="errorMessage">失败时面向用户的短说明。</param>
    public static bool TryLoad(string absolutePath, out CareerPathSkillPackage? package, out string? errorMessage)
    {
        package = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            errorMessage = "未指定技能包文件路径。";
            return false;
        }

        try
        {
            var full = Path.GetFullPath(absolutePath);
            if (!File.Exists(full))
            {
                errorMessage = $"找不到文件：{full}";
                return false;
            }

            var json = File.ReadAllText(full, Encoding.UTF8);
            var parsed = JsonSerializer.Deserialize<CareerPathSkillPackage>(json, Options);
            if (parsed is null)
            {
                errorMessage = "JSON 根对象为空或无法识别。";
                return false;
            }

            package = parsed;
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = "JSON 格式无效：" + ex.Message;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            errorMessage = "没有权限读取该技能包文件。";
            return false;
        }
        catch (IOException ex)
        {
            errorMessage = "读取文件失败：" + ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = "加载技能包失败：" + ex.Message;
            return false;
        }
    }
}
