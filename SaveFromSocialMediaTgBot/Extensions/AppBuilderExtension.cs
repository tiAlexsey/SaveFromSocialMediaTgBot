using System.Net;
using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Interfaces;
using SaveFromSocialMediaTgBot.Logging;
using SaveFromSocialMediaTgBot.Services;
using SaveFromSocialMediaTgBot.Services.Scraper;
using Serilog;

namespace SaveFromSocialMediaTgBot.Extensions;

public static class AppBuilderExtension
{
    internal static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration[EnvironmentConstants.RedisConnectionString];
            options.InstanceName = "Chat-settings:";
        });

        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }

    internal static IServiceCollection AddVideoScrapers(this IServiceCollection services)
    {
        // Puppeteer client for instagram
        new BrowserFetcher().DownloadAsync();

        var launchOptions = new LaunchOptions()
        {
            Headless = true,
            ExecutablePath = "/usr/bin/chromium",
            Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
        };

        services.AddSingleton<IBrowser>(sp => Puppeteer.LaunchAsync(launchOptions).GetAwaiter().GetResult());
        services.AddHttpClient<IScraper, InstagramScraper>();
        services.AddHttpClient<IScraper, TwitterScraper>();
        services.AddHttpClient<IScraper, YoutubeScraper>();
        services.AddHttpClient<IScraper, TiktokScraper>(client =>
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

        return services;
    }

    internal static IServiceCollection ConfigureLogging(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Filter.ByIncludingOnly(evt =>
            {
                if (evt.Properties.TryGetValue("SourceContext", out var ctx))
                {
                    var source = ctx.ToString().Trim('"');
                    if (source.StartsWith("System.Net.Http.HttpClient.IVideoScraper.LogicalHandler") ||
                        source.StartsWith("System.Net.Http.HttpClient.IVideoScraper.ClientHandler"))
                    {
                        return evt.Level >= Serilog.Events.LogEventLevel.Warning;
                    }
                }

                return true;
            })
            .Enrich.With<RequestContextEnricher>()
            .CreateLogger();

        services.AddSerilog();

        return services;
    }
}