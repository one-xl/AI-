using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiSmartDrill.App.Drill.Ai.Ark;

namespace AiSmartDrill.App.Drill.Ai.Client;

/// <summary>
/// 聊天消息
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 角色
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 内容（响应体中可能为字符串或多段 JSON，由 <see cref="ArkChatContentStringConverter"/> 统一为文本）。
    /// </summary>
    [JsonConverter(typeof(ArkChatContentStringConverter))]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 推理内容
    /// </summary>
    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; }
}

/// <summary>
/// 工具定义
/// </summary>
public class ToolDefinition
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数定义
    /// </summary>
    public ToolParameters Parameters { get; set; } = new ToolParameters();
}

/// <summary>
/// 工具参数
/// </summary>
public class ToolParameters
{
    /// <summary>
    /// 参数类型
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// 必需参数
    /// </summary>
    public List<string> Required { get; set; } = new List<string>();

    /// <summary>
    /// 属性定义
    /// </summary>
    public Dictionary<string, ParameterProperty> Properties { get; set; } = new Dictionary<string, ParameterProperty>();
}

/// <summary>
/// 参数属性
/// </summary>
public class ParameterProperty
{
    /// <summary>
    /// 属性类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 属性描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值
    /// </summary>
    public List<string>? Enum { get; set; }
}

/// <summary>
/// 聊天完成响应
/// </summary>
public class ChatCompletionResponse
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 对象
    /// </summary>
    public string Object { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public long Created { get; set; }

    /// <summary>
    /// 模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 服务等级
    /// </summary>
    public string? ServiceTier { get; set; }

    /// <summary>
    /// 选择
    /// </summary>
    public List<Choice> Choices { get; set; } = new List<Choice>();

    /// <summary>
    /// 使用情况
    /// </summary>
    public Usage? Usage { get; set; }
}

/// <summary>
/// 选择
/// </summary>
public class Choice
{
    /// <summary>
    /// 索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public ChatMessage Message { get; set; } = new ChatMessage();

    /// <summary>
    /// 完成原因
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;

    /// <summary>
    /// 对数概率
    /// </summary>
    public object? Logprobs { get; set; }

    /// <summary>
    /// 工具调用
    /// </summary>
    public ToolCall? ToolCall { get; set; }
}

/// <summary>
/// 工具调用
/// </summary>
public class ToolCall
{
    /// <summary>
    /// 工具调用 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 工具名称
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 工具调用参数
    /// </summary>
    public ToolCallFunction Function { get; set; } = new ToolCallFunction();
}

/// <summary>
/// 工具调用函数
/// </summary>
public class ToolCallFunction
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 函数参数
    /// </summary>
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// 使用情况
/// </summary>
public class Usage
{
    /// <summary>
    /// 提示令牌
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// 完成令牌
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// 总令牌
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// 提示令牌详情
    /// </summary>
    public PromptTokensDetails? PromptTokensDetails { get; set; }

    /// <summary>
    /// 完成令牌详情
    /// </summary>
    public CompletionTokensDetails? CompletionTokensDetails { get; set; }
}

/// <summary>
/// 提示令牌详情
/// </summary>
public class PromptTokensDetails
{
    /// <summary>
    /// 缓存令牌
    /// </summary>
    [JsonPropertyName("cached_tokens")]
    public int CachedTokens { get; set; }
}

/// <summary>
/// 完成令牌详情
/// </summary>
public class CompletionTokensDetails
{
    /// <summary>
    /// 推理令牌
    /// </summary>
    [JsonPropertyName("reasoning_tokens")]
    public int ReasoningTokens { get; set; }
}

/// <summary>
/// 聊天完成流式响应
/// </summary>
public class ChatCompletionStreamResponse
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 对象
    /// </summary>
    public string Object { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public long Created { get; set; }

    /// <summary>
    /// 模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 选择
    /// </summary>
    public List<StreamChoice> Choices { get; set; } = new List<StreamChoice>();
}

/// <summary>
/// 流式选择
/// </summary>
public class StreamChoice
{
    /// <summary>
    /// 索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 增量消息
    /// </summary>
    public ChatMessage? Delta { get; set; }

    /// <summary>
    /// 完成原因
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    /// <summary>
    /// 工具调用
    /// </summary>
    public ToolCall? ToolCall { get; set; }
}

/// <summary>
/// /responses 端点响应模型
/// </summary>
public class DoubaoResponse
{
    /// <summary>
    /// 响应 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 对象类型
    /// </summary>
    public string Object { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public long Created { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 输出内容
    /// </summary>
    public List<DoubaoOutputItem>? Output { get; set; }

    /// <summary>
    /// 使用情况
    /// </summary>
    public Usage? Usage { get; set; }
}

/// <summary>
/// /responses 端点输出项
/// </summary>
public class DoubaoOutputItem
{
    /// <summary>
    /// 角色
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public List<DoubaoContentItem>? Content { get; set; }
}

/// <summary>
/// /responses 端点内容项
/// </summary>
public class DoubaoContentItem
{
    /// <summary>
    /// 类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 文本内容
    /// </summary>
    public string? Text { get; set; }
}
