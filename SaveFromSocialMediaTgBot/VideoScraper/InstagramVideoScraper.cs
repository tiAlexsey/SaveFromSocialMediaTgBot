using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Data.Const;
using SaveFromSocialMediaTgBot.Services;
using System.Text.RegularExpressions;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class InstagramVideoScraper
{
    private readonly string _login;
    private readonly string _password;
    private readonly Regex _pattern = new(@"""https:\S+?\.mp4\S+?""", RegexOptions.Compiled);

    private readonly LaunchOptions _launchOptions = new()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium",
        Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
    };

    public InstagramVideoScraper(IConfiguration configuration)
    {
        _login = configuration["INSTA_LOGIN"];
        _password = configuration["INSTA_PASSWORD"];
    }

    public async Task<string> GetVideoUrlAsync(string pageUrl)
    {
        var result = await TryGetVideoUrlAsync(pageUrl);

        if (result == null)
        {
            throw new FormatException(Messages.ERROR_EMPTY_URL);
        }

        return result;
    }

    private async Task<string?> TryGetVideoUrlAsync(string pageUrl)
    {
        // Запускаем браузер в headless-режиме
        await using var browser = await Puppeteer.LaunchAsync(_launchOptions);
        // Открываем новую страницу в браузере
        await using var page = await browser.NewPageAsync();

        await page.SetCookieAsync(InstagramAuthService.Cookies);
        var tryCount = 0;

        Match? match = null;
        while (tryCount++ <= 1)
        {
            // Переходим по URL
            await page.GoToAsync(pageUrl, WaitUntilNavigation.Networkidle0);
            // develop
            var fileName = $"Screenshot-{Regex.Match(pageUrl, "igsh=[^&]+")}.png";
            Console.WriteLine(fileName);
            await page.ScreenshotAsync(fileName);
            // Выкачиваем html страницу
            var content = await page.GetContentAsync();
            // Закрываем браузер
            await browser.CloseAsync();

            match = _pattern.Match(content);
            if (!match.Success)
            {
                await page.SetCookieAsync(await InstagramAuthService.UpdateCookies(_login, _password));
            }
            else
            {
                tryCount++;
            }
        }

        if (match.Success)
        {
            return match.Value
                .Trim('"')
                .Replace("\\", "");
        }

        return null;
    }
}