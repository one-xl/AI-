using System.Text.Json.Serialization;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// CareerPath AI（Python/Streamlit）导出的 UTF-8 JSON 技能包根对象（扩展名常为 .skillpkg）。
/// </summary>
public sealed class CareerPathSkillPackage
{
    /// <summary>
    /// 格式版本，当前对接为 <c>2.0</c>。
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 来源标识，约定为 <c>careerpath_ai</c>（可选校验）。
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// <c>direct</c> 或 <c>ai_recommend</c>。
    /// </summary>
    [JsonPropertyName("practice_mode")]
    public string PracticeMode { get; set; } = "direct";

    /// <summary>
    /// 从岗位 JD 分析出的知识点短语列表。
    /// </summary>
    [JsonPropertyName("skills")]
    public string[] Skills { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 岗位上下文（难度、简述等）。
    /// </summary>
    [JsonPropertyName("job_context")]
    public CareerPathJobContext? JobContext { get; set; }

    /// <summary>
    /// ISO8601 时间字符串。
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// 可选：刷题筛选与题量（实习通网页可写入；未提供时使用软件内默认）。
    /// </summary>
    [JsonPropertyName("exam_options")]
    public CareerPathExamOptions? ExamOptions { get; set; }
}

/// <summary>
/// 技能包中可选的考试/刷题筛选参数。
/// </summary>
public sealed class CareerPathExamOptions
{
    /// <summary>
    /// 领域显示名提示，与界面「领域」下拉一致，如「Python」「数据库」；无法识别时忽略。
    /// </summary>
    [JsonPropertyName("domain_hint")]
    public string? DomainHint { get; set; }

    /// <summary>
    /// 难度：简单 / 中等 / 困难；空表示不限制。
    /// </summary>
    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }

    /// <summary>
    /// 本次组卷题量（1～50）。
    /// </summary>
    [JsonPropertyName("question_count")]
    public int? QuestionCount { get; set; }
}

/// <summary>
/// 技能包中的岗位上下文。
/// </summary>
public sealed class CareerPathJobContext
{
    /// <summary>
    /// 期望难度，如「简单」「中等」「困难」。
    /// </summary>
    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>
    /// 岗位简述，供 AI 推荐模块使用。
    /// </summary>
    [JsonPropertyName("job_summary")]
    public string JobSummary { get; set; } = string.Empty;
}
