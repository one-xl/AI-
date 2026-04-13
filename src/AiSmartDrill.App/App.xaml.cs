using System.Windows;
using AiSmartDrill.App.Drill.Ai;
using AiSmartDrill.App.Drill.Ai.Client;
using AiSmartDrill.App.Drill.Ai.Config;
using AiSmartDrill.App.Drill.Ai.Tools;
using AiSmartDrill.App.Drill.Import;
using AiSmartDrill.App.Infrastructure;
using AiSmartDrill.App.ViewModels;
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
    /// 应用启动：构建配置与 DI 容器，初始化数据库并显示主窗口。
    /// </summary>
    /// <param name="e">启动参数。</param>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var connectionString = configuration.GetConnectionString("Default")
                               ?? "Data Source=AiSmartDrill.db";

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<DatabaseInitializer>();

        services.AddHttpClient();

        // 配置豆包模型选项
        services.Configure<DoubaoModelOptions>(configuration.GetSection(DoubaoModelOptions.SectionName));
        services.AddSingleton<DoubaoModelConfig>();
        services.AddHttpClient<IChatCompletionService, DoubaoApiClient>();

        // 注册工具
        services.AddHttpClient<NetworkTool>();
        services.AddSingleton<FileTool>();
        services.AddSingleton<CommandTool>();
        services.AddSingleton<ToolManager>();

        services.AddSingleton<IAiTutorService, ApiAiTutorService>();
        services.AddSingleton<IQuestionRecommendationService, ApiQuestionRecommendationService>();
        services.AddSingleton<IStudyPlanService, ApiStudyPlanService>();
        services.AddSingleton<QuestionImportService>();

        services.AddSingleton<MainWindowViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var initializer = _serviceProvider.GetRequiredService<DatabaseInitializer>();
        initializer.InitializeAsync().GetAwaiter().GetResult();

        var mainVm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();
    }

    /// <summary>
    /// 应用退出：释放 DI 容器资源。
    /// </summary>
    /// <param name="e">退出参数。</param>
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
