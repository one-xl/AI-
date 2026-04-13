using System.Collections.Generic;
using System.Threading.Tasks;
using AiSmartDrill.App.Drill.Ai.Client;

namespace AiSmartDrill.App.Drill.Ai.Tools;

/// <summary>
/// 工具管理器，用于管理所有工具
/// </summary>
public class ToolManager
{
    private readonly Dictionary<string, ITool> _tools = new();

    /// <summary>
    /// 注册工具
    /// </summary>
    /// <param name="tool">工具实例</param>
    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// 获取所有工具
    /// </summary>
    /// <returns>工具列表</returns>
    public IEnumerable<ITool> GetAllTools()
    {
        return _tools.Values;
    }

    /// <summary>
    /// 获取所有工具定义
    /// </summary>
    /// <returns>工具定义列表</returns>
    public IEnumerable<ToolDefinition> GetAllToolDefinitions()
    {
        foreach (var tool in _tools.Values)
        {
            yield return tool.GetToolDefinition();
        }
    }

    /// <summary>
    /// 根据名称获取工具
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <returns>工具实例</returns>
    public ITool? GetTool(string name)
    {
        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <param name="parameters">工具参数</param>
    /// <returns>执行结果</returns>
    public async Task<string> ExecuteToolAsync(string name, string parameters)
    {
        var tool = GetTool(name);
        if (tool == null)
        {
            return $"工具 {name} 不存在";
        }

        return await tool.ExecuteAsync(parameters);
    }
}