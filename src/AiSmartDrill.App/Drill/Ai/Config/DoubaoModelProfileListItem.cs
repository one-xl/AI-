namespace AiSmartDrill.App.Drill.Ai.Config;

/// <summary>
/// 供界面绑定的模型档案列表项（Id + 展示文案）。
/// </summary>
public sealed class DoubaoModelProfileListItem
{
    /// <summary>
    /// 初始化列表项。
    /// </summary>
    public DoubaoModelProfileListItem(string id, string displayLabel)
    {
        Id = id;
        DisplayLabel = displayLabel;
    }

    /// <summary>
    /// 配置键，与 <c>Profiles</c> 中一致。
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 下拉框展示用文本。
    /// </summary>
    public string DisplayLabel { get; }

    /// <inheritdoc />
    public override string ToString() => DisplayLabel;
}
