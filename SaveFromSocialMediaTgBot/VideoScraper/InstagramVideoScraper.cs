using System.Text.RegularExpressions;
using PuppeteerSharp;
using PuppeteerSharp.Mobile;
using SaveFromSocialMediaTgBot.Data.Const;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class InstagramVideoScraper
{
    private readonly Regex _pattern = new(@"""https:\S+\.mp4\S+""", RegexOptions.Compiled);
    private readonly DeviceDescriptor _device = Puppeteer.Devices[DeviceDescriptorName.IPhone4];
    private readonly int _retryCount;

    public InstagramVideoScraper(IConfiguration configuration)
    {
        _retryCount = int.TryParse(configuration["RETRY_COUNT"], out var retryCount) ? retryCount : 1;
    }

    private readonly LaunchOptions _launchOptions = new()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium",
        Args = ["--disable-gpu", "--no-sandbox", "--disable-setuid-sandbox"]
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
        // Переходим по URL
        await page.GoToAsync(pageUrl);
        // develop
        await page.ScreenshotAsync($"{pageUrl.Replace(":", "")}.png");
        // Ждем окно авторизации
        await page.WaitForSelectorAsync("div[role='button']");
        // Закрываем окно
        await page.ClickAsync("div[role='button']");
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