using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows;
using AiSmartDrill.App.ViewModels;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App.CareerPath;

/// <summary>
/// 单实例场景下，第二个进程通过命名管道将技能包参数投递给已运行的主窗口，避免重复打开多份刷题软件。
/// </summary>
public static class CareerPathIpc
{
    private const int AllowAnyProcessToSetForeground = -1;

    /// <summary>
    /// 与 <see cref="StartListener"/> 使用相同的管道名（本机）。
    /// </summary>
    public const string PipeName = "AiSmartDrill.CareerPath.v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// 将当前进程已解析的 <see cref="CareerPathStartupState"/> 发送给主实例后，本进程应退出。
    /// </summary>
    /// <returns>是否成功连接并写入。</returns>
    public static bool TrySendToRunningInstance()
    {
        TryAllowForegroundWindow();

        var import = CareerPathStartupState.ImportPath;
        var mode = CareerPathStartupState.ModeFromCli;
        var auto = CareerPathStartupState.AutoProceedFromCli;
        var payload = new CareerPathIpcPayload
        {
            ImportPath = import,
            ModeCli = mode,
            AutoProceed = auto,
            ActivateOnly = string.IsNullOrWhiteSpace(import)
        };

        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(
                    ".",
                    PipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous);
                client.Connect(TimeSpan.FromSeconds(3));
                var json = JsonSerializer.Serialize(payload, JsonOptions);
                using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine(json);
                return true;
            }
            catch
            {
                if (attempt == 7)
                {
                    return false;
                }

                Thread.Sleep(250);
            }
        }

        return false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    private static void TryAllowForegroundWindow()
    {
        try
        {
            AllowSetForegroundWindow(AllowAnyProcessToSetForeground);
        }
        catch
        {
            // 忽略前台权限放行失败，仍继续尝试 IPC。
        }
    }

    /// <summary>
    /// 在主实例启动主窗口后调用，循环接受子进程投递的 <see cref="CareerPathIpcPayload"/>。
    /// </summary>
    public static void StartListener(MainWindowViewModel mainVm, ILogger logger)
    {
        if (Application.Current?.Dispatcher is null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync().ConfigureAwait(false);
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    CareerPathIpcPayload? payload;
                    try
                    {
                        payload = JsonSerializer.Deserialize<CareerPathIpcPayload>(line, JsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        logger.LogWarning(ex, "CareerPath IPC JSON 无效");
                        continue;
                    }

                    if (payload is null)
                    {
                        continue;
                    }

                    var pathResolved = payload.ActivateOnly || string.IsNullOrWhiteSpace(payload.ImportPath)
                        ? null
                        : payload.ImportPath;
                    _ = Application.Current!.Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            await mainVm.ProcessCareerPathImportAsync(
                                    pathResolved,
                                    payload.ModeCli,
                                    payload.AutoProceed)
                                .ConfigureAwait(true);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "CareerPath IPC 处理失败");
                            MessageBox.Show(
                                "收到网页传来的技能包，但处理失败：" + ex.Message,
                                "CareerPath",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    });
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "CareerPath IPC 服务端循环异常");
                }
            }
        });
    }
}
