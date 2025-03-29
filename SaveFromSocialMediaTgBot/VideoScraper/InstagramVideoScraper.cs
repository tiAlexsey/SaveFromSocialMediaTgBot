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
        string? result = null;
        var i = 0;
        while (i < _retryCount && result is null)
        {
            result = await TryGetVideoUrlAsync(pageUrl);

            i++;
        }

        if (result == null)
        {
            throw new FormatException(Messages.ERROR_EMPTY_URL);
        }

        Console.WriteLine("got link in {0} attemps", i);
        return result;
    }

    private async Task<string?> TryGetVideoUrlAsync(string pageUrl)
    {
        // Запускаем браузер в headless-режиме
        await using var browser = await Puppeteer.LaunchAsync(_launchOptions);
        await using var page = await browser.NewPageAsync();

        await page.SetUserAgentAsync("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.50 Safari/537.36");
        await page.EmulateAsync(_device);

        // Переходим по URL
        await page.GoToAsync(pageUrl, WaitUntilNavigation.Networkidle0);

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