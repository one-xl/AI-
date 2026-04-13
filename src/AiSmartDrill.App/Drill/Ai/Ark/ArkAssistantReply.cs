using System.Linq;
using AiSmartDrill.App.Drill.Ai.Client;

namespace AiSmartDrill.App.Drill.Ai.Ark;

/// <summary>
/// 从 OpenAI 兼容的 <see cref="ChatCompletionResponse"/> 中取出助手文本（含部分模型的 <c>reasoning_content</c> 回退拼接）。
/// </summary>
public static class ArkAssistantReply
{
    /// <summary>
    /// 获取第一条 choice 的助手可见文本内容。
    /// </summary>
    public static string GetPrimaryText(ChatCompletionResponse? response)
    {
        var msg = response?.Choices?.FirstOrDefault()?.Message;
        if (msg == null)
            return string.Empty;

        var text = msg.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(msg.ReasoningContent))
            return msg.ReasoningContent!.Trim();

        return text;
    }
}
