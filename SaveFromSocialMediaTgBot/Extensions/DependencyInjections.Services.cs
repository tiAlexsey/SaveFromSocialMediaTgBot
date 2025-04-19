using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Abstract.Interface;
using SaveFromSocialMediaTgBot.Services;
using SaveFromSocialMediaTgBot.VideoScraper;
using SaveFromSocialMediaTgBot.Workers;

namespace SaveFromSocialMediaTgBot.Extensions;

public static partial class DependencyInjections
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        new BrowserFetcher().DownloadAsync();

        services.AddTransient<InstagramVideoScraper>();
        services.AddTransient<TiktokVideoScraper>();
        services.AddTransient<TwitterVideoScraper>();
        services.AddTransient<YoutubeVideoScraper>();
        services.AddTransient<ScraperService>();
        services.AddCache(configuration);

        services.AddHostedService<TelegramBotWorker>();
    }
    
    private static void AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("REDIS_CONNECTION_STRING");
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "Chat-settings:";
        });

        services.AddSingleton<ICacheService, CacheService>();
    }
}