using System.Net;
using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Interfaces;
using SaveFromSocialMediaTgBot.Services;
using SaveFromSocialMediaTgBot.Services.VideoScraper;

namespace SaveFromSocialMediaTgBot.Extensions;

public static partial class DependencyInjections
{
    private static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        new BrowserFetcher().DownloadAsync();
        services.AddVideoScrapers();
        services.AddCache(configuration);
        services.AddTransient<ITelegramBotService, TelegramBotService>();

        services.AddHostedService<TelegramBotWorker>();
    }

    private static void AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration[EnvironmentConstants.REDIS_CONNECTION_STRING];
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "Chat-settings:";
        });

        services.AddSingleton<ICacheService, CacheService>();
    }

    private static void AddVideoScrapers(this IServiceCollection services)
    {
        services.AddHttpClient<IVideoScraper, InstagramVideoScraper>();
        services.AddHttpClient<IVideoScraper, TwitterVideoScraper>();
        services.AddHttpClient<IVideoScraper, YoutubeVideoScraper>();
        services.AddHttpClient<IVideoScraper, TiktokVideoScraper>(client =>
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        });

        services.AddSingleton<ScraperService>();
    }
}