using System.Text.RegularExpressions;
using PuppeteerSharp;
using PuppeteerSharp.Mobile;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class InstagramVideoScraper
{
    private readonly Regex _pattern = new(@"""https:\S+\.mp4\S+""", RegexOptions.Compiled);

    private readonly DeviceDescriptor
        _device = Puppeteer.Devices[DeviceDescriptorName.IPhone4]; // Эмулируем мобильный девайс

    private readonly LaunchOptions _launchOptions = new() // Указываем нативный путь к Chromium для ARM64
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium", // проверьте, что Chromium установлен по этому пути
        Args = ["--disable-gpu", "--no-sandbox", "--disable-setuid-sandbox"]
    };

    public async Task<Stream> GetVideoStreamAsync(string pageUrl)
    {
        var content = await GetContentAsync(pageUrl);
        string link;

        // Находим ссылку на видео
        var match = _pattern.Match(content);
        if (match.Success)
        {
            link = match.Value.Trim('"')
                .Replace("amp;", "")
                .Replace("\\", "");
        }
        else
        {
            throw new FormatException("Invalid page URL");
        }


        HttpClient client = new();
        return await client.GetStreamAsync(link);
    }

    private async Task<string> GetContentAsync(string pageUrl)
    {
        // Запускаем браузер в headless-режиме
        using var browser = await Puppeteer.LaunchAsync(_launchOptions);
        using var page = await browser.NewPageAsync();

        await page.EmulateAsync(_device);

        // Переходим по URL
        await page.GoToAsync(pageUrl, WaitUntilNavigation.Networkidle0);

        var content = await page.GetContentAsync();
        // Закрываем браузер
        await browser.CloseAsync();

        return content;
    }
}