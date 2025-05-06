using System.Net;
using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Abstract.Interface;
using SaveFromSocialMediaTgBot.Data.Const;
using SaveFromSocialMediaTgBot.Services;
using SaveFromSocialMediaTgBot.VideoScraper;

namespace SaveFromSocialMediaTgBot.Extensions;

public static partial class DependencyInjections
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        new BrowserFetcher().DownloadAsync();
        services.AddVideoScrapers();
        services.AddCache(configuration);
        services.AddTransient<ITelegramBotService, TelegramBotService>();
    }

    private static void AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration[Env.REDIS_CONNECTION_STRING];
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
        });

        services.AddSingleton<ScraperService>();
    }
}