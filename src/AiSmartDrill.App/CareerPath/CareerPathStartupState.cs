namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 由 <see cref="App"/> 在启动时解析命令行后写入，供主窗口消费一次技能包导入流程。
/// 外部工具（如实习通 Streamlit）可传 <c>--import</c>、<c>--mode</c>、<c>--auto</c>。
/// </summary>
public static class CareerPathStartupState
{
    /// <summary>
    /// <c>--import</c> 给出的技能包路径（可为 null 表示未通过 CLI 导入）。
    /// </summary>
    public static string? ImportPath { get; set; }

    /// <summary>
    /// <c>--mode</c> 原始字符串（可为 null）。
    /// </summary>
    public static string? ModeFromCli { get; set; }

    /// <summary>
    /// 若命令行包含 <c>--auto</c>，则技能包导入后不弹出确认框，直接进入刷题或 AI 推荐并尽量自动开考。
    /// </summary>
    public static bool AutoProceedFromCli { get; set; }

    /// <summary>
    /// 自定义协议参数解析失败时，面向用户显示的错误文本。
    /// </summary>
    public static string? ProtocolActivationError { get; set; }

    /// <summary>
    /// 启动参数解析器：支持 <c>--import "path"</c>、<c>--mode direct|ai-recommend</c> 与 <c>--auto</c>。
    /// </summary>
    public static void ApplyCommandLineArgs(string[]? args)
    {
        ImportPath = null;
        ModeFromCli = null;
        AutoProceedFromCli = false;
        ProtocolActivationError = null;
        if (args is null || args.Length == 0)
        {
            return;
        }

        if (CareerPathProtocolActivation.TryApply(args, out var protocolError))
        {
            return;
        }

        ProtocolActivationError = protocolError;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a.Equals("--import", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                ImportPath = Unquote(args[++i]);
            }
            else if (a.Equals("--mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                ModeFromCli = Unquote(args[++i]);
            }
            else if (a.Equals("--auto", StringComparison.OrdinalIgnoreCase))
            {
                AutoProceedFromCli = true;
            }
        }
    }

    private static string Unquote(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
        {
            return s[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
        }

        return s;
    }
}
