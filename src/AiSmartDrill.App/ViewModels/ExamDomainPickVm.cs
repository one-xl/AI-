using AiSmartDrill.App.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiSmartDrill.App.ViewModels;

/// <summary>
/// 「考试 / 刷题」组卷时可勾选的单个领域项，多选后与题型、难度筛选组合抽题。
/// </summary>
public partial class ExamDomainPickVm : ObservableObject
{
    /// <summary>
    /// 初始化领域勾选项。
    /// </summary>
    /// <param name="domain">领域枚举值。</param>
    /// <param name="displayName">与题库编辑器一致的显示名称。</param>
    public ExamDomainPickVm(QuestionDomain domain, string displayName)
    {
        Domain = domain;
        DisplayName = displayName;
    }

    /// <summary>
    /// 获取领域枚举值。
    /// </summary>
    public QuestionDomain Domain { get; }

    /// <summary>
    /// 获取界面展示名称。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 是否勾选：参与组卷领域集合。
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
}
