using System.Net;
using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Interfaces;
using SaveFromSocialMediaTgBot.Logging;
using SaveFromSocialMediaTgBot.Services;
using SaveFromSocialMediaTgBot.Services.VideoScraper;
using Serilog;
using Telegram.Bot;

namespace SaveFromSocialMediaTgBot.Extensions;

public static class AppBuilderExtension
{
    public static void AddDependencyInjections(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.ConfigureLogging(configuration);

        services.AddSingleton<ITelegramBotClient, TelegramBotClient>(_ =>
        {
            var options = new TelegramBotClientOptions(configuration[EnvironmentConstants.BOT_TOKEN]!);
            return new TelegramBotClient(options);
        });

        services.AddCache(configuration);
        services.AddTransient<ITelegramBotService, TelegramBotService>();
        services.AddVideoScrapers();

        services.AddHostedService<TelegramBotWorker>();
    }

    private static void AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration[EnvironmentConstants.REDIS_CONNECTION_STRING];
            options.InstanceName = "Chat-settings:";
        });

        services.AddSingleton<ICacheService, CacheService>();
    }

    private static void AddVideoScrapers(this IServiceCollection services)
    {
        // Puppeteer client for instagram
        new BrowserFetcher().DownloadAsync();

        services.AddHttpClient<IVideoScraper, InstagramVideoScraper>();
        services.AddHttpClient<IVideoScraper, TwitterVideoScraper>();
        services.AddHttpClient<IVideoScraper, YoutubeVideoScraper>();
        services.AddHttpClient<IVideoScraper, TiktokVideoScraper>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AllowAutoRedirect = true
            });

        services.AddSingleton<ScraperService>();
    }

    private static void ConfigureLogging(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.With<RequestContextEnricher>()
            .CreateLogger();

        services.AddSerilog();
    }
}