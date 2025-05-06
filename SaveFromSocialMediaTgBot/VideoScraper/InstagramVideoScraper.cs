using System.Text.RegularExpressions;
using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Abstract.Interface;
using SaveFromSocialMediaTgBot.Data.Const;
using SaveFromSocialMediaTgBot.Extensions.Helper;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class InstagramVideoScraper(IConfiguration configuration, HttpClient client) : IVideoScraper
{
    private readonly Regex pattern = new(Pattern.INSTAGRAM, RegexOptions.Compiled);
    private readonly string login = configuration[Env.INST_LOGIN] ?? "";
    private readonly string password = configuration[Env.INST_PASSWORD] ?? "";
    private string sessionId = configuration[Env.INST_COOKIE_SESSION_ID] ?? "";

    private readonly LaunchOptions launchOptions = new()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium",
        Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
    };

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
        // Ставим  куки
        await SetCookies(page);

        var tryCount = 0;

        try
        {
            Match? match = null;
            while (tryCount++ <= 1)
            {
                // Переходим по URL
                await page.GoToAsync(pageUrl, WaitUntilNavigation.Networkidle0);
                // Выкачиваем html страницу
                var content = await page.GetContentAsync();

                match = pattern.Match(content);
                if (!match.Success)
                {
                    await page.SetCookieAsync(await InstagramHelper.AuthorizationAsync(login, password));
                }
                else
                {
                    tryCount++;
                }
            }

            // Закрываем браузер
            await browser.CloseAsync();

            if (match.Success)
            {
                return match.Value
                    .Trim('"')
                    .Replace("\\", "");
            }
        }
        catch (Exception ex)
        {
            var fileName = $"Screenshot-{Regex.Match(pageUrl, "igsh=[^&]+")}.png";
            Console.WriteLine(fileName);
            await page.ScreenshotAsync(fileName);
        }

        return null;
    }

    private async Task SetCookies(IPage page)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        InstagramHelper.Cookies =
        [
            new CookieParam
            {
                Name = "sessionid",
                Value = sessionId,
                Domain = ".instagram.com"
            }
        ];
        sessionId = string.Empty;

        await page.SetCookieAsync(InstagramHelper.Cookies);
    }
}