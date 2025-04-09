using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Services;
using SaveFromSocialMediaTgBot.VideoScraper;
using SaveFromSocialMediaTgBot.Workers;

namespace SaveFromSocialMediaTgBot.Extensions;

public static partial class DependencyInjections
{
    public static void AddServices(this IServiceCollection services)
    {
        new BrowserFetcher().DownloadAsync();

        services.AddTransient<InstagramVideoScraper>();
        services.AddTransient<TiktokVideoScraper>();
        services.AddTransient<TwitterVideoScraper>();
        services.AddTransient<YoutubeVideoScraper>();
        services.AddTransient<ScraperService>();

        services.AddHostedService<TelegramBotWorker>();
    }
}