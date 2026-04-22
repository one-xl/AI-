using System;
using System.Windows;
using System.Windows.Controls;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.ViewModels;
using System.Threading.Tasks;

namespace AiSmartDrill.App;

/// <summary>
/// 主窗口：承载 Tab 导航与各模块 UI。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 「考试 / 刷题」在 <see cref="MainTabControl"/> 中的索引（与 XAML 中 Tab 顺序一致）。
    /// </summary>
    private const int ExamTabIndex = 1;

    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// 订阅 ViewModel 事件：考试开始时切换到考试标签页。
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ExamStarted += ViewModel_ExamStarted;
            await vm.ProcessCareerPathStartupIfAnyAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// 任意方式成功开考后，切换到「考试 / 刷题」页（含随机组卷、错题再练、推荐题开考）。
    /// </summary>
    private void ViewModel_ExamStarted(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => MainTabControl.SelectedIndex = ExamTabIndex);
    }

    /// <summary>
    /// 窗口关闭时释放视图模型资源（停止计时器等）。
    /// </summary>
    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ExamStarted -= ViewModel_ExamStarted;
            if (viewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// 表格内「分类标签」列结束编辑时写回数据库，与右侧编辑器保持同步。
    /// </summary>
    private async void BankQuestionsDataGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Column is not DataGridTextColumn col || col.Header as string != "分类标签")
        {
            return;
        }

        if (e.EditAction == DataGridEditAction.Cancel)
        {
            return;
        }

        if (e.Row.Item is not Question row || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var text = e.EditingElement is TextBox tb ? tb.Text : row.TopicTags;
        try
        {
            await vm.SaveBankQuestionTopicTagsAsync(row.Id, text).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show("保存分类标签失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
