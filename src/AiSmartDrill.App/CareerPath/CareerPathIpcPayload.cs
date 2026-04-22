namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 通过命名管道在「第二个进程」与「已运行的主实例」之间传递 CareerPath 启动参数（JSON 一行）。
/// </summary>
public sealed class CareerPathIpcPayload
{
    /// <summary>
    /// 为 true 且 <see cref="ImportPath"/> 为空时，仅将主窗口置前（用户重复启动 exe 且无 --import）。
    /// </summary>
    public bool ActivateOnly { get; set; }

    /// <summary>
    /// 技能包 UTF-8 JSON 文件绝对路径（与 CLI <c>--import</c> 一致）。
    /// </summary>
    public string? ImportPath { get; set; }

    /// <summary>
    /// CLI <c>--mode</c> 原始值（可为 null）。
    /// </summary>
    public string? ModeCli { get; set; }

    /// <summary>
    /// 是否对应 CLI <c>--auto</c>。
    /// </summary>
    public bool AutoProceed { get; set; }
}
