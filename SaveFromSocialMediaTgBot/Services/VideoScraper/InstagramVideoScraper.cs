using System.Text.RegularExpressions;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Interfaces;

namespace SaveFromSocialMediaTgBot.Services.VideoScraper;

public class InstagramVideoScraper(IConfiguration configuration, HttpClient client) : IVideoScraper
{
    private readonly Random random = new();
    private readonly Regex pattern = new(PatternConstants.INSTAGRAM, RegexOptions.Compiled);
    private readonly string login = configuration[EnvironmentConstants.INST_LOGIN] ?? "";
    private readonly string password = configuration[EnvironmentConstants.INST_PASSWORD] ?? "";

    private readonly LaunchOptions launchOptions = new()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium",
        Args =
        [
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--disable-dev-shm-usage",
            "--disable-blink-features=AutomationControlled",
            "--disable-infobars",
            "--lang=en-US,en;q=0.9"
        ]
    };

    private static CookieParam[]? Cookies { get; set; }

    public bool CanHandle(string url)
        => url.Contains("instagram.com", StringComparison.OrdinalIgnoreCase);

    public async Task<Stream> GetVideoStreamAsync(string url)
    {
        var videoUrl = await TryGetVideoUrlAsync(url) ?? throw new FormatException(MessageConstants.ERROR_EMPTY_URL);
        return await client.GetStreamAsync(videoUrl);
    }

    private async Task<string?> TryGetVideoUrlAsync(string pageUrl)
    {
        // Запускаем браузер в headless-режиме
        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        // Открываем новую страницу в браузере
        await using var page = await browser.NewPageAsync();

        await page.EvaluateFunctionOnNewDocumentAsync(@"() => {
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            window.chrome = { runtime: {} };
            Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3] });
        }");

        await page.SetUserAgentAsync(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");

        // Ставим куки
        await SetCookiesAsync(page);

        var tryCount = 0;
        try
        {
            Match? match = null;
            while (tryCount++ <= 1)
            {
                // Переходим по URL
                await page.GoToAsync(pageUrl,
                    new NavigationOptions { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] });
                // Выкачиваем html страницу
                var content = await page.GetContentAsync();

                match = pattern.Match(content);
                if (!match.Success)
                {
                    await page.SetCookieAsync(await AuthorizationAsync(page));
                    continue;
                }

                tryCount++;
            }

            // Закрываем браузер
            await browser.CloseAsync();

            if (match!.Success)
            {
                return match.Value
                    .Trim('"')
                    .Replace("\\", "");
            }
        }
        catch
        {
            var fileName = $"Screenshot-{Regex.Match(pageUrl, "igsh=[^&]+")}.png";
            Console.WriteLine(fileName);
            await page.ScreenshotAsync(fileName);
        }

        return null;
    }

    private async Task SetCookiesAsync(IPage page)
    {
        await page.SetCookieAsync(Cookies ?? await AuthorizationAsync(page));
    }

    private async Task<CookieParam[]> AuthorizationAsync(IPage page)
    {
        // Переход на страницу входа Instagram
        var response = await page.GoToAsync("https://www.instagram.com/accounts/login/",
            new NavigationOptions { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] });
        Console.WriteLine(response.Status);

        // Ожидание появления полей ввода
        await page.WaitForSelectorAsync("input[name='username']");
        await page.WaitForSelectorAsync("input[name='password']");

        await Task.Delay(random.Next(800, 1000));
        // Ввод данных
        await page.TypeAsync("input[name='username']", login, new TypeOptions { Delay = 150 });
        await Task.Delay(random.Next(500, 1000));
        await page.TypeAsync("input[name='password']", password, new TypeOptions { Delay = 150 });

        // Нажатие кнопки входа
        await page.ClickAsync("button[type='submit']");
        // Ждем авторизацию
        await page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] });
        await Task.Delay(random.Next(1000, 3000));
        Cookies = await page.GetCookiesAsync();
        return Cookies;
    }
}