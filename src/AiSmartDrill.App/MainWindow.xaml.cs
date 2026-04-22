using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AiSmartDrill.App.Domain;
using AiSmartDrill.App.ViewModels;

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
    /// 题库刷新后仅为前几行触发轻量入场动画，避免全量逐条动画带来额外开销。
    /// </summary>
    private const int AnimatedBankRowCount = 6;

    private bool _pendingBankRefreshAnimation;
    private readonly HashSet<int> _animatedBankRowIndexes = new();

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
            vm.PropertyChanged += ViewModel_PropertyChanged;
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
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            if (viewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(MainWindowViewModel.BankQuestions), StringComparison.Ordinal) ||
            !_pendingBankRefreshAnimation)
        {
            return;
        }

        _animatedBankRowIndexes.Clear();
        Dispatcher.InvokeAsync(BankQuestionsDataGrid.UpdateLayout);
    }

    private void RefreshBankButton_OnClick(object sender, RoutedEventArgs e)
    {
        _pendingBankRefreshAnimation = true;
        _animatedBankRowIndexes.Clear();
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

    private void BankQuestionsDataGrid_OnLoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (!_pendingBankRefreshAnimation)
        {
            return;
        }

        var rowIndex = e.Row.GetIndex();
        if (rowIndex < 0 || rowIndex >= AnimatedBankRowCount || !_animatedBankRowIndexes.Add(rowIndex))
        {
            return;
        }

        AnimateEntrance(e.Row, rowIndex, 8, 34);
        if (_animatedBankRowIndexes.Count >= AnimatedBankRowCount)
        {
            _pendingBankRefreshAnimation = false;
        }
    }

    private void ExamDomainPickerPopup_OnOpened(object sender, EventArgs e)
    {
        if (sender is not Popup popup || popup.Child is not FrameworkElement child)
        {
            return;
        }

        var checkBoxes = FindVisualChildren<CheckBox>(child)
            .Where(x => Equals(x.Style, Resources["ExamDomainOptionCheckBoxStyle"]))
            .ToList();

        for (var i = 0; i < checkBoxes.Count; i++)
        {
            AnimateEntrance(checkBoxes[i], i, 10, 40);
        }
    }

    private static void AnimateEntrance(FrameworkElement element, int order, double offsetY, double delayMs)
    {
        element.Opacity = 0;

        if (element.RenderTransform is not TranslateTransform translate)
        {
            translate = new TranslateTransform();
            element.RenderTransform = translate;
        }

        translate.Y = offsetY;
        var beginTime = TimeSpan.FromMilliseconds(order * delayMs);

        var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            BeginTime = beginTime,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var moveAnimation = new DoubleAnimation(offsetY, 0, TimeSpan.FromMilliseconds(260))
        {
            BeginTime = beginTime,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        element.BeginAnimation(OpacityProperty, opacityAnimation, HandoffBehavior.SnapshotAndReplace);
        translate.BeginAnimation(TranslateTransform.YProperty, moveAnimation, HandoffBehavior.SnapshotAndReplace);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
