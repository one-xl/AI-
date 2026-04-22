using System.Linq;
using System.Windows;
using AiSmartDrill.App.CareerPath;

namespace AiSmartDrill.App;

/// <summary>
/// 当 CareerPath 技能点在题库中无匹配题时，收集题目生成数量、题型与难度的对话框。
/// </summary>
public partial class CareerPathGenerateQuestionsDialog : Window
{
    /// <summary>
    /// 用户确认后的题目数量。
    /// </summary>
    public int GenerationCount { get; private set; }

    /// <summary>
    /// 用户确认后的题型显示名。
    /// </summary>
    public string SelectedQuestionType { get; private set; } = "单选";

    /// <summary>
    /// 用户确认后的难度显示名。
    /// </summary>
    public string SelectedDifficulty { get; private set; } = "全部";

    /// <summary>
    /// 用户选择的领域显示名（与题库领域枚举对应）。
    /// </summary>
    public string SelectedDomainDisplay { get; private set; } = "未分类";

    /// <summary>
    /// 初始化对话框。
    /// </summary>
    public CareerPathGenerateQuestionsDialog(
        IReadOnlyList<string> domainChoices,
        string defaultDomainDisplay,
        string domainStateText,
        IReadOnlyList<string> skills,
        int defaultCount = 20)
    {
        InitializeComponent();
        foreach (var d in domainChoices)
        {
            CbDomain.Items.Add(d);
        }

        CbDomain.SelectedItem = domainChoices.Contains(defaultDomainDisplay)
            ? defaultDomainDisplay
            : domainChoices.FirstOrDefault() ?? "未分类";
        SelectedDomainDisplay = (CbDomain.SelectedItem as string)?.Trim() ?? "未分类";

        DataContext = new
        {
            DomainStateText = domainStateText,
            SkillsPreview = string.Join(Environment.NewLine, skills),
            DefaultCount = Math.Clamp(defaultCount, 1, 96).ToString()
        };
        GenerationCount = Math.Clamp(defaultCount, 1, 96);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TbCount.Text.Trim(), out var count) || count < 1 || count > 96)
        {
            MessageBox.Show("生成数量必须是 1 到 96 之间的整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        GenerationCount = count;
        SelectedQuestionType = ((CbType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "单选").Trim();
        SelectedDifficulty = ((CbDifficulty.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "全部").Trim();
        SelectedDomainDisplay = (CbDomain.SelectedItem as string)?.Trim() ?? "未分类";
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
