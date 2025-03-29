using System.Text.RegularExpressions;
using PuppeteerSharp;
using PuppeteerSharp.Mobile;
using SaveFromSocialMediaTgBot.Data.Const;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class InstagramVideoScraper
{
    private readonly Regex _pattern = new(@"""https:\S+\.mp4\S+""", RegexOptions.Compiled);
    private readonly DeviceDescriptor _device = Puppeteer.Devices[DeviceDescriptorName.IPhone4];

    private readonly LaunchOptions _launchOptions = new()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium",
        Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
    };

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
        // Эмулируем мобильное устройство
        await page.EmulateAsync(_device);
        await page.EvaluateFunctionAsync(@"() => {Object.defineProperty(navigator, 'webdriver', { get: () => false });}");
        // Переходим по URL
        await page.GoToAsync(pageUrl, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
        });
        
        // develop
        var fileName = $"Screenshot-{Regex.Match(pageUrl, "igsh=[^&]+")}.png";
        Console.WriteLine(fileName);
        await page.ScreenshotAsync(fileName);
        // Ждем окно авторизации
        await page.WaitForSelectorAsync("div[role='button']");
        // Закрываем окно
        await page.ClickAsync("div[role='button']");
        await Task.Delay(3210);
        // Перезагружаем страницу с видео
        await page.GoToAsync(pageUrl, WaitUntilNavigation.Networkidle0);
        // Выкачиваем html страницу
        var content = await page.GetContentAsync();
        // Закрываем браузер
        await browser.CloseAsync();

        var match = _pattern.Match(content);
        if (match.Success)
        {
            return match.Value
                .Trim('"')
                .Replace("amp;", "")
                .Replace("\\", "");
        }

        return null;
    }
}