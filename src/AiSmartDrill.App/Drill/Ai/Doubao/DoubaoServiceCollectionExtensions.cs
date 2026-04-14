using AiSmartDrill.App;
using AiSmartDrill.App.Drill.Ai.Client;
using AiSmartDrill.App.Drill.Ai.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiSmartDrill.App.Drill.Ai.Doubao;

/// <summary>
/// 将火山方舟豆包（OpenAI 兼容 <c>/chat/completions</c>）客户端注册到依赖注入容器。
/// </summary>
public static class DoubaoServiceCollectionExtensions
{
    /// <summary>
    /// 注册 <see cref="DoubaoModelOptions"/>、<see cref="DoubaoModelConfig"/> 与 <see cref="IChatCompletionService"/> → <see cref="ArkChatCompletionClient"/>（命名 HttpClient）。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用程序配置（需包含 <see cref="DoubaoModelOptions.SectionName"/> 节）。</param>
    /// <returns>同一 <paramref name="services"/> 实例，便于链式调用。</returns>
    public static IServiceCollection AddDoubaoArkLanguageModel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOptions<DoubaoModelOptions>>(_ =>
        {
            var o = new DoubaoModelOptions();
            configuration.GetSection(DoubaoModelOptions.SectionName).Bind(o);
            o.NormalizeProfilesAfterBind();
            UserDoubaoProfileStore.MergeInto(o);
            return Options.Create(o);
        });
        services.AddSingleton<DoubaoModelConfig>();
        services.AddHttpClient<IChatCompletionService, ArkChatCompletionClient>();
        return services;
    }
}
