using AiSmartDrill.App;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiSmartDrill.App.ViewModels;

/// <summary>
/// 错题本表格中的一行：展示题干、错答/标答，并支持勾选参与 AI 解析（勾选状态由 ViewModel 与短时记忆集合同步）。
/// </summary>
public partial class WrongBookRowVm : ObservableObject
{
    private readonly Action<long, bool>? _onSelectionChanged;
    private bool _suppressSelectionCallback;

    /// <summary>
    /// 初始化错题行。
    /// </summary>
    public WrongBookRowVm(
        long wrongBookEntryId,
        long questionId,
        string domainDisplay,
        string typeDisplay,
        string stem,
        string? optionsJson,
        string wrongAnswerDisplay,
        string standardAnswerDisplay,
        int wrongCount,
        DateTime lastWrongAtUtc,
        DateTime? lastRedoCompletedAtUtc,
        Action<long, bool>? onSelectionChanged)
    {
        WrongBookEntryId = wrongBookEntryId;
        QuestionId = questionId;
        DomainDisplay = domainDisplay;
        TypeDisplay = typeDisplay;
        Stem = stem;
        OptionsDisplay = QuestionOptionsDisplayFormatter.FormatForDisplay(optionsJson);
        WrongAnswerDisplay = wrongAnswerDisplay;
        StandardAnswerDisplay = standardAnswerDisplay;
        WrongCount = wrongCount;
        LastWrongAtUtc = lastWrongAtUtc;
        LastWrongAtDisplay = lastWrongAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        RedoTagDisplay = lastRedoCompletedAtUtc.HasValue
            ? "已重做"
            : "—";
        RedoTagToolTip = lastRedoCompletedAtUtc.HasValue
            ? $"完成错题再练交卷：{lastRedoCompletedAtUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm}"
            : "尚未完成一次错题再练交卷";
        _onSelectionChanged = onSelectionChanged;
    }

    /// <summary>
    /// 错题本表主键（用于删除）。
    /// </summary>
    public long WrongBookEntryId { get; }

    /// <summary>
    /// 题目 Id。
    /// </summary>
    public long QuestionId { get; }

    /// <summary>
    /// 领域显示名。
    /// </summary>
    public string DomainDisplay { get; }

    /// <summary>
    /// 题型显示名。
    /// </summary>
    public string TypeDisplay { get; }

    /// <summary>
    /// 题干全文。
    /// </summary>
    public string Stem { get; }

    /// <summary>
    /// 客观题选项全文（由 <c>OptionsJson</c> 展开为多行 A/B/C…）。
    /// </summary>
    public string OptionsDisplay { get; }

    /// <summary>
    /// 最近一次错误作答展示文本。
    /// </summary>
    public string WrongAnswerDisplay { get; }

    /// <summary>
    /// 标准答案。
    /// </summary>
    public string StandardAnswerDisplay { get; }

    /// <summary>
    /// 累计错次。
    /// </summary>
    public int WrongCount { get; }

    /// <summary>
    /// 最近错误时间（UTC）。
    /// </summary>
    public DateTime LastWrongAtUtc { get; }

    /// <summary>
    /// 最近错误时间（本地格式化）。
    /// </summary>
    public string LastWrongAtDisplay { get; }

    /// <summary>
    /// 是否已做过错题再练（简短标签）。
    /// </summary>
    public string RedoTagDisplay { get; }

    /// <summary>
    /// 重做标签的补充说明（悬停查看时间）。
    /// </summary>
    public string RedoTagToolTip { get; }

    /// <summary>
    /// 是否勾选参与 AI 解析。
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        if (!_suppressSelectionCallback)
        {
            _onSelectionChanged?.Invoke(QuestionId, value);
        }
    }

    /// <summary>
    /// 从短时记忆恢复勾选状态时不触发回调，避免重复写入集合。
    /// </summary>
    public void SyncSelectionFromMemory(bool selected)
    {
        if (IsSelected == selected)
        {
            return;
        }

        _suppressSelectionCallback = true;
        IsSelected = selected;
        _suppressSelectionCallback = false;
    }
}
