using System;
using System.Windows;

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
    }

    /// <summary>
    /// 窗口关闭时释放视图模型资源（停止计时器等）。
    /// </summary>
    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
