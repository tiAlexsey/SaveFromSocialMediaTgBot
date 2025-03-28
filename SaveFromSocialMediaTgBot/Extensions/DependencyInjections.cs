using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Services;
using SaveFromSocialMediaTgBot.VideoScrapper;

namespace SaveFromSocialMediaTgBot.Extensions;

public static class DependencyInjections
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddTransient<InstagramVideoScraper>();
        services.AddTransient<TiktokVideoScraper>();
        services.AddTransient<TwitterVideoScraper>();
        services.AddTransient<ScraperService>();
        new BrowserFetcher().DownloadAsync();
    }
}