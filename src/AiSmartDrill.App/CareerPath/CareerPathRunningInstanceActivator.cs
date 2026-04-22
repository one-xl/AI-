using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 在检测到已有主实例运行时，由第二个进程直接尝试将现有主窗口恢复并置前。
/// 这样可以利用“用户刚刚点击了网页协议链接”带来的前台权限窗口。
/// </summary>
public static class CareerPathRunningInstanceActivator
{
    private const int ShowWindowRestore = 9;
    private const uint SetWindowPosFlagsNoMoveOrSize = 0x0001 | 0x0002;
    private const uint SetWindowPosFlagsShowWindow = 0x0040;
    private static readonly IntPtr TopMostWindowHandle = new(-1);
    private static readonly IntPtr NotTopMostWindowHandle = new(-2);

    /// <summary>
    /// 尝试将已运行实例的主窗口拉到前台。
    /// </summary>
    public static bool TryActivateRunningInstanceWindow()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            var processName = Path.GetFileNameWithoutExtension(current.MainModule?.FileName) ?? current.ProcessName;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                foreach (var proc in Process.GetProcessesByName(processName))
                {
                    if (proc.Id == current.Id)
                    {
                        continue;
                    }

                    var handle = proc.MainWindowHandle;
                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    ShowWindow(handle, ShowWindowRestore);
                    BringWindowToTop(handle);
                    SetWindowPos(handle, TopMostWindowHandle, 0, 0, 0, 0, SetWindowPosFlagsNoMoveOrSize | SetWindowPosFlagsShowWindow);
                    SetWindowPos(handle, NotTopMostWindowHandle, 0, 0, 0, 0, SetWindowPosFlagsNoMoveOrSize | SetWindowPosFlagsShowWindow);
                    SetForegroundWindow(handle);
                    if (GetForegroundWindow() != handle)
                    {
                        var flashInfo = new FLASHWINFO
                        {
                            cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                            hwnd = handle,
                            dwFlags = FlashTray | FlashTimerNoForeground,
                            uCount = 3,
                            dwTimeout = 0
                        };
                        FlashWindowEx(ref flashInfo);
                    }
                    return true;
                }

                Thread.Sleep(150);
            }
        }
        catch
        {
            // 忽略置前失败，保留给主实例自身继续尝试。
        }

        return false;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    private const uint FlashTray = 0x00000002;
    private const uint FlashTimerNoForeground = 0x0000000C;
}
