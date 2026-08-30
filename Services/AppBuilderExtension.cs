using System.Net;
using Abstract.Data.Constants;
using Abstract.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using Services.Scrapers;

namespace Services;

public static class AppBuilderExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddVideoScrapers()
        {
            // Puppeteer client for instagram
            new BrowserFetcher().DownloadAsync();

            var launchOptions = new LaunchOptions
            {
                Headless = true,
                ExecutablePath = "/usr/bin/google-chrome",
                Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
            };

            services.AddSingleton<IBrowser>(_ => Puppeteer.LaunchAsync(launchOptions).GetAwaiter().GetResult());
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

        public IServiceCollection AddCache(IConfiguration configuration, string instance)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration[EnvironmentConstants.RedisConnectionString];
                options.InstanceName = instance;
            });

            services.AddSingleton<ICacheService, CacheService>();

            return services;
        }
    }
}