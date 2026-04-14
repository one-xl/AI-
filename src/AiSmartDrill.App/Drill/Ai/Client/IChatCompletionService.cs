using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Client;

/// <summary>
/// 火山方舟豆包文本对话抽象：OpenAI 兼容 <c>POST /api/v3/chat/completions</c>，由 <see cref="ArkChatCompletionClient"/> 实现。
/// </summary>
public interface IChatCompletionService
{
    /// <summary>
    /// 生成聊天完成响应
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="tools">工具列表</param>
    /// <param name="maxTokens">覆盖配置中的单次回复 token 上限；为 <c>null</c> 时使用全局配置。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>聊天完成响应</returns>
    Task<ChatCompletionResponse> GenerateCompletionAsync(
        IList<ChatMessage> messages,
        IList<ToolDefinition>? tools = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 流式生成聊天完成响应
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="tools">工具列表</param>
    /// <param name="maxTokens">覆盖配置中的单次回复 token 上限；为 <c>null</c> 时使用全局配置。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流式响应枚举器</returns>
    IAsyncEnumerable<ChatCompletionStreamResponse> GenerateCompletionStreamAsync(
        IList<ChatMessage> messages,
        IList<ToolDefinition>? tools = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);
}
