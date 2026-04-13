using System;
using System.Windows;
using System.Windows.Controls;
using AiSmartDrill.App.ViewModels;

namespace AiSmartDrill.App;

/// <summary>
/// 主窗口：承载 Tab 导航与各模块 UI。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// 窗口加载完成后订阅 ViewModel 事件。
    /// </summary>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ExamStarted += ViewModel_ExamStarted;
        }
    }

    /// <summary>
    /// 当考试开始时，自动切换到"考试/刷题"标签页。
    /// </summary>
    private void ViewModel_ExamStarted(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            MainTabControl.SelectedIndex = 1;
        });
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
}
