using System.Threading.Tasks;

namespace AiSmartDrill.App.Drill.Ai.Sandbox;

/// <summary>
/// 沙箱环境接口，定义沙箱的基本方法
/// </summary>
public interface ISandbox
{
    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <param name="args">命令参数</param>
    /// <returns>执行结果</returns>
    Task<string> ExecuteCommandAsync(string command, string[] args);

    /// <summary>
    /// 读取文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件内容</returns>
    Task<string> ReadFileAsync(string path);

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="content">文件内容</param>
    /// <returns>执行结果</returns>
    Task<string> WriteFileAsync(string path, string content);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件是否存在</returns>
    Task<bool> FileExistsAsync(string path);

    /// <summary>
    /// 检查目录是否存在
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>目录是否存在</returns>
    Task<bool> DirectoryExistsAsync(string path);

    /// <summary>
    /// 创建目录
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>执行结果</returns>
    Task<string> CreateDirectoryAsync(string path);

    /// <summary>
    /// 列出目录内容
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>目录内容</returns>
    Task<string> ListDirectoryAsync(string path);
}