using System.Linq;
using System.Text;
using System.Windows;
using AiSmartDrill.App.CareerPath;
using AiSmartDrill.App.Domain;

namespace AiSmartDrill.App;

/// <summary>
/// CareerPath 直接刷题前的准备：展示知识点/领域命中统计，并选择领域、难度与题量。
/// </summary>
public partial class CareerPathPracticePrepareDialog : Window
{
    /// <summary>
    /// 领域下拉中的显示串；「全部」表示不限制领域。
    /// </summary>
    public string SelectedDomainUi { get; private set; } = "全部";

    /// <summary>
    /// 难度显示串，含「全部」。
    /// </summary>
    public string SelectedDifficultyUi { get; private set; } = "全部";

    /// <summary>
    /// 本次组卷题量。
    /// </summary>
    public int QuestionCount { get; private set; } = 5;

    public CareerPathPracticePrepareDialog(
        IReadOnlyDictionary<string, int> perSkillCounts,
        IReadOnlyDictionary<QuestionDomain, int> byDomainCounts,
        string inferredDomainLine,
        int defaultQuestionCount,
        string defaultDomainUi,
        string defaultDifficultyUi)
    {
        InitializeComponent();

        TbPerSkill.Text = BuildPerSkillText(perSkillCounts);
        TbByDomain.Text = BuildByDomainText(byDomainCounts);
        TbInfer.Text = inferredDomainLine;

        foreach (var ui in BuildDomainItems())
        {
            CbDomain.Items.Add(ui);
        }

        CbDomain.SelectedItem = defaultDomainUi is { } d && CbDomain.Items.Contains(d)
            ? d
            : "全部";

        foreach (var x in new[] { "全部", "简单", "中等", "困难" })
        {
            CbDifficulty.Items.Add(x);
        }

        CbDifficulty.SelectedItem = new[] { "全部", "简单", "中等", "困难" }.Contains(defaultDifficultyUi)
            ? defaultDifficultyUi
            : "全部";

        TbCount.Text = Math.Clamp(defaultQuestionCount, 1, 50).ToString();
    }

    /// <summary>
    /// 与主窗口领域下拉一致的显示名列表（含「全部」）。
    /// </summary>
    public static IReadOnlyList<string> GetDomainUiOptionsForFilter()
    {
        return BuildDomainItems().ToList();
    }

    private static IEnumerable<string> BuildDomainItems()
    {
        yield return "全部";
        yield return "未分类";
        yield return "Python";
        yield return "C";
        yield return "C++";
        yield return "C#";
        yield return "Rust";
        yield return "Java";
        yield return "JavaScript";
        yield return "Go";
        yield return "数据结构与算法";
        yield return "数据库";
        yield return "操作系统";
        yield return "计算机网络";
    }

    private static string BuildPerSkillText(IReadOnlyDictionary<string, int> perSkillCounts)
    {
        if (perSkillCounts.Count == 0)
        {
            return "（无技能项）";
        }

        var sb = new StringBuilder();
        foreach (var kv in perSkillCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"• {kv.Key}：{kv.Value} 题");
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildByDomainText(IReadOnlyDictionary<QuestionDomain, int> byDomainCounts)
    {
        if (byDomainCounts.Count == 0)
        {
            return "当前题库中尚无命中这些知识点的题目。";
        }

        var sb = new StringBuilder();
        foreach (var kv in byDomainCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
        {
            sb.AppendLine($"{CareerPathDomainInference.MapDomainDisplay(kv.Key)}：{kv.Value} 题");
        }

        return sb.ToString().TrimEnd();
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TbCount.Text.Trim(), out var n) || n < 1 || n > 50)
        {
            MessageBox.Show("题量必须是 1～50 的整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedDomainUi = (CbDomain.SelectedItem as string)?.Trim() ?? "全部";
        SelectedDifficultyUi = (CbDifficulty.SelectedItem as string)?.Trim() ?? "全部";
        QuestionCount = n;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
