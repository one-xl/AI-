using System.IO;
using System.Threading;
using System.Windows;
using AiSmartDrill.App.CareerPath;
using AiSmartDrill.App.Drill.Ai;
using AiSmartDrill.App.Drill.Ai.Config;
using AiSmartDrill.App.Drill.Ai.Doubao;
using AiSmartDrill.App.Drill.Import;
using AiSmartDrill.App.Infrastructure;
using AiSmartDrill.App.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiSmartDrill.App;

/// <summary>
/// WPF 应用程序入口：负责配置加载、依赖注入容器构建与数据库初始化。
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// 单实例互斥：第二个进程在转发技能包后退出，不重复打开主窗口。
    /// </summary>
    private static Mutex? _singleInstanceMutex;

    /// <summary>
    /// 应用启动：构建配置与 DI 容器，初始化数据库并显示主窗口。
    /// </summary>
    /// <remarks>
    /// 实习通等外部工具通过命令行唤醒刷题流程时，须先于其他逻辑调用
    /// <see cref="CareerPathStartupState.ApplyCommandLineArgs"/> 解析 <c>--import</c>、<c>--mode</c>、<c>--auto</c>。
    /// 若本机已有实例，第二个进程通过命名管道 <see cref="CareerPathIpc"/> 将参数投递给主窗口后退出。
    /// 主窗口 Loaded 后由 <see cref="ViewModels.MainWindowViewModel.ProcessCareerPathStartupIfAnyAsync"/> 消费冷启动参数；
    /// 后续网页再次唤醒时由 <see cref="ViewModels.MainWindowViewModel.ProcessCareerPathImportAsync"/> 处理。
    /// </remarks>
    /// <param name="e">启动参数。</param>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        CareerPathStartupState.ApplyCommandLineArgs(e.Args);
        if (!string.IsNullOrWhiteSpace(CareerPathStartupState.ProtocolActivationError))
        {
            MessageBox.Show(
                CareerPathStartupState.ProtocolActivationError,
                "AiSmartDrill",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown(1);
            return;
        }

        const string mutexName = @"Local\AiSmartDrill.CareerPath.SingleInstance.v1";
        _singleInstanceMutex = new Mutex(true, mutexName, out var createdNew);
        if (!createdNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            var forwarded = CareerPathIpc.TrySendToRunningInstance();
            CareerPathRunningInstanceActivator.TryActivateRunningInstanceWindow();
            if (!forwarded)
            {
                MessageBox.Show(
                    "刷题软件已在运行，但未能将本次技能包发送给已打开的窗口。\n请切换到刷题软件继续操作，或关闭后再从网页启动。",
                    "AiSmartDrill",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            Shutdown(0);
            return;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<App>(optional: true, reloadOnChange: true)
            .Build();

        DayNightThemeBootstrap.ApplyStartupTheme(configuration);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var connectionString = NormalizeSqliteConnectionString(
            configuration.GetConnectionString("Default")
            ?? "Data Source=AiSmartDrill.db");

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<DatabaseInitializer>();

        services.AddSingleton<AiCallTrace>();
        services.AddDoubaoArkLanguageModel(configuration);

        services.AddSingleton<IAiTutorService, ApiAiTutorService>();
        services.AddSingleton<IQuestionRecommendationService, ApiQuestionRecommendationService>();
        services.AddSingleton<IStudyPlanService, ApiStudyPlanService>();
        services.AddSingleton<ApiQuestionTeachingService>();
        services.AddSingleton<IQuestionBankAiGenerationService>(sp => sp.GetRequiredService<ApiQuestionTeachingService>());
        services.AddSingleton<IExamQuestionAiExplainService>(sp => sp.GetRequiredService<ApiQuestionTeachingService>());
        services.AddSingleton<QuestionImportService>();

        services.AddSingleton<MainWindowViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var startupLogger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        CareerPathProtocolRegistration.EnsureRegistered(startupLogger);
        var doubaoCfg = _serviceProvider.GetRequiredService<DoubaoModelConfig>();
        if (!doubaoCfg.IsValid())
        {
            startupLogger.LogWarning(
                "Doubao 未配置 ApiKey 或 ModelName：请在 appsettings.json 或 User Secrets 中设置 DoubaoModel:ApiKey 与 DoubaoModel:ModelName（一般为推理接入点 ep-...）。AI 功能将失败或回退。");
        }
        else
        {
            startupLogger.LogInformation("Doubao Ark 已配置，当前档案：{Profile}", doubaoCfg.ActiveProfileId);
        }

        var mainVm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow { DataContext = mainVm };
        Current.MainWindow = mainWindow;
        mainWindow.Show();

        var initializer = _serviceProvider.GetRequiredService<DatabaseInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();

        var ipcLogger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CareerPathIpc");
        CareerPathIpc.StartListener(mainVm, ipcLogger);
    }

    /// <summary>
    /// 将 SQLite 相对路径锚定到程序目录，避免协议拉起时工作目录变化导致无法打开数据库文件。
    /// </summary>
    private static string NormalizeSqliteConnectionString(string connectionString)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            var dataSource = builder.DataSource;
            if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:" || Path.IsPathRooted(dataSource))
            {
                return builder.ToString();
            }

            var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dataSource));
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            builder.DataSource = fullPath;
            return builder.ToString();
        }
        catch
        {
            return connectionString;
        }
    }

    /// <summary>
    /// 应用退出：释放 DI 容器资源。
    /// </summary>
    /// <param name="e">退出参数。</param>
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _singleInstanceMutex?.Dispose();
        }
        catch
        {
            // ignore
        }

        _singleInstanceMutex = null;
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
