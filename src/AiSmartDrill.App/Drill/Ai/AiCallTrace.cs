namespace AiSmartDrill.App.Drill.Ai;

/// <summary>
/// 记录最近一次 AI 相关调用是否真正命中火山方舟 HTTP API（供 UI 区分「云端成功」与「本地回退」）。
/// </summary>
public sealed class AiCallTrace
{
    /// <summary>
    /// 最近一次步骤说明，例如 <c>tutor:ark</c>、<c>recommend:local</c>。
    /// </summary>
    public string LastStep { get; private set; } = string.Empty;

    /// <summary>
    /// 最近一次是否走了方舟 API（含完整成功解析）；若为 <c>false</c> 表示本地模板/规则或仅部分云端。
    /// </summary>
    public bool LastUsedArk { get; private set; }

    /// <summary>
    /// 标记一次调用结果。
    /// </summary>
    /// <param name="step">步骤标识。</param>
    /// <param name="usedArk">是否实际请求并成功使用方舟返回。</param>
    public void Set(string step, bool usedArk)
    {
        LastStep = step;
        LastUsedArk = usedArk;
    }
}
