using System.Threading.Tasks;
using AiSmartDrill.App.Drill.Ai.Client;

namespace AiSmartDrill.App.Drill.Ai.Tools;

/// <summary>
/// 工具接口，定义工具的基本方法
/// </summary>
public interface ITool
{
    /// <summary>
    /// 工具名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 工具描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="parameters">工具参数</param>
    /// <returns>执行结果</returns>
    Task<string> ExecuteAsync(string parameters);

    /// <summary>
    /// 获取工具定义，用于 AI 模型调用
    /// </summary>
    /// <returns>工具定义</returns>
    Client.ToolDefinition GetToolDefinition();
}