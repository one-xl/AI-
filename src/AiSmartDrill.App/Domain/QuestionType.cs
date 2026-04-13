namespace AiSmartDrill.App.Domain;

/// <summary>
/// 表示题目在业务层中的题型分类，用于筛选、组卷与判分策略分支。
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// 单项选择题，标准答案通常为选项键（例如 A/B/C）。
    /// </summary>
    SingleChoice = 0,

    /// <summary>
    /// 多项选择题，标准答案通常为逗号分隔的选项键集合。
    /// </summary>
    MultipleChoice = 1,

    /// <summary>
    /// 判断题，标准答案通常为“对/错”或“true/false”。
    /// </summary>
    TrueFalse = 2,

    /// <summary>
    /// 简答题，标准答案为参考答案文本，判分时采用宽松规范化比对。
    /// </summary>
    ShortAnswer = 3,

    /// <summary>
    /// 填空题等主观输入题，标准答案字段存放用于判分的正则表达式（匹配用户输入即视为正确）。
    /// </summary>
    FillInBlank = 4
}
