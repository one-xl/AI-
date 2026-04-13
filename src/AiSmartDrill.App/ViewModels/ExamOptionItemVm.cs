using CommunityToolkit.Mvvm.ComponentModel;

namespace AiSmartDrill.App.ViewModels;

/// <summary>
/// 考试中客观题选项按钮对应的一行视图状态（单选/多选共用，通过 <see cref="IsSelected"/> 表示是否选中）。
/// </summary>
public partial class ExamOptionItemVm : ObservableObject
{
    /// <summary>
    /// 选项键（A、B、C…），与标准答案格式一致。
    /// </summary>
    [ObservableProperty]
    private string _key = string.Empty;

    /// <summary>
    /// 展示文本（含键与选项正文）。
    /// </summary>
    [ObservableProperty]
    private string _caption = string.Empty;

    /// <summary>
    /// 是否选中：单选题为当前答案；多选题为已勾选集合。
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
}
