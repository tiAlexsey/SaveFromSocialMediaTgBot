using System.Text.RegularExpressions;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using SaveFromSocialMediaTgBot.Abstract.Interface;
using SaveFromSocialMediaTgBot.Data.Const;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class InstagramVideoScraper(IConfiguration configuration, HttpClient client) : IVideoScraper
{
    private readonly Random random = new();
    private readonly Regex pattern = new(Pattern.INSTAGRAM, RegexOptions.Compiled);
    private readonly string login = configuration[Env.INST_LOGIN] ?? "";
    private readonly string password = configuration[Env.INST_PASSWORD] ?? "";

    private readonly LaunchOptions launchOptions = new()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium",
        Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
    };

    private static CookieParam[] Cookies { get; set; } = [];

    public bool CanHandle(string url)
        => url.Contains("instagram.com", StringComparison.OrdinalIgnoreCase);

    public async Task<Stream> GetVideoStreamAsync(string url)
    {
        var videoUrl = await TryGetVideoUrlAsync(url) ?? throw new FormatException(Messages.ERROR_EMPTY_URL);
        return await client.GetStreamAsync(videoUrl);
    }

    private async Task<string?> TryGetVideoUrlAsync(string pageUrl)
    {
        // Запускаем браузер в headless-режиме
        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        // Открываем новую страницу в браузере
        await using var page = await browser.NewPageAsync();
        // Ставим куки
        await SetCookiesAsync(page);

        var tryCount = 0;
        try
        {
            Match? match = null;
            while (tryCount++ <= 1)
            {
                // Переходим по URL
                await page.GoToAsync(pageUrl, WaitUntilNavigation.Networkidle0);
                await Task.Delay(random.Next(500, 1000));
                // Выкачиваем html страницу
                var content = await page.GetContentAsync();

                match = pattern.Match(content);
                if (!match.Success)
                {
                    await page.SetCookieAsync(await AuthorizationAsync());
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

    private static async Task SetCookiesAsync(IPage page)
    {
        await page.SetCookieAsync(Cookies);
    }

    private async Task<CookieParam[]> AuthorizationAsync()
    {
        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();

        // Переход на страницу входа Instagram
        await page.GoToAsync("https://www.instagram.com/accounts/login/");

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
        await page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = [WaitUntilNavigation.Networkidle0] });
        await Task.Delay(random.Next(1000, 3000));
        Cookies = await page.GetCookiesAsync();
        return Cookies;
    }
}