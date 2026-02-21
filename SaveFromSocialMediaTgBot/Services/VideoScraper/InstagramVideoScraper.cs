using System.Text.Json;
using System.Text.RegularExpressions;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Interfaces;

namespace SaveFromSocialMediaTgBot.Services.VideoScraper;

public class InstagramVideoScraper(
    ILogger<InstagramVideoScraper> logger,
    IConfiguration configuration,
    HttpClient client) : IVideoScraper
{
    private readonly Random random = new();
    private readonly Regex pattern = new(PatternConstants.INSTAGRAM, RegexOptions.Compiled);
    private readonly string login = configuration[EnvironmentConstants.INST_LOGIN] ?? "";
    private readonly string password = configuration[EnvironmentConstants.INST_PASSWORD] ?? "";
    private string sessionId = configuration[EnvironmentConstants.INST_COOKIE_SESSION_ID] ?? "";
    private readonly NavigationOptions navigationOptions = new() { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] };
    private readonly TypeOptions typeOptions = new() { Delay = 150 };

    private readonly LaunchOptions launchOptions = new()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium",
        Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
    };

    private static CookieParam[]? Cookies { get; set; }

    public bool CanHandle(string url) => url.Contains("instagram.com", StringComparison.OrdinalIgnoreCase);

    public async Task<Stream> GetVideoStreamAsync(string url)
    {
        var videoUrl = await TryGetVideoUrlAsync(url) ?? throw new FormatException(MessageConstants.ERROR_EMPTY_URL);
        return await client.GetStreamAsync(videoUrl);
    }

    private async Task<string?> TryGetVideoUrlAsync(string pageUrl)
    {
        logger.LogInformation("Start getting video url from {PageUrl}", pageUrl);

        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();

        try
        {
            logger.LogDebug("Setting cookies");
            await SetCookiesAsync(page);

            for (var i = 0; i < 2; i++)
            {
                logger.LogInformation("Attempt {Attempt} to load page", i + 1);

                await page.GoToAsync(pageUrl, navigationOptions);

                var content = await page.GetContentAsync();

                logger.LogDebug("Page loaded. Content length: {Length}", content.Length);

                var match = pattern.Match(content);

                // test
                var matches = pattern.Matches(content);

                if (matches.Count == 0)
                {
                    logger.LogWarning("No regex matches found");
                }
                else
                {
                    foreach (Match m in matches)
                    {
                        logger.LogInformation("Match found: {Match}", m.Value);
                        for (int g = 1; g < m.Groups.Count; g++)
                        {
                            logger.LogInformation("Group {GroupNumber}: {GroupValue}", g, m.Groups[g].Value);
                        }
                    }
                }
                // test 

                if (match.Success)
                {
                    var findUrl = match.Value.Replace("&amp;", "&");

                    logger.LogInformation("Video url found: {VideoUrl}", findUrl);

                    return findUrl;
                }

                logger.LogWarning("Video url not found on attempt {Attempt}", i + 1);

                if (i == 0)
                {
                    logger.LogInformation("Trying to re-authorize (cookies may be expired)");
                    await page.SetCookieAsync(await AuthorizationAsync(page));
                }
            }
        }
        catch (Exception ex)
        {
            var fileName = $"Screenshot-{Regex.Match(pageUrl, "igsh=[^&]+")}.Value.png";

            logger.LogError(ex, "Error while trying to get video url from {PageUrl}. Screenshot: {Screenshot}",
                pageUrl, fileName);

            await page.ScreenshotAsync(fileName);
            throw;
        }

        logger.LogWarning("Video url not found after all attempts for {PageUrl}", pageUrl);
        return null;
    }

    private async Task SetCookiesAsync(IPage page)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            logger.LogInformation("Applying session cookie");

            Cookies =
            [
                new CookieParam
                {
                    Name = "sessionid",
                    Value = sessionId,
                    Domain = ".instagram.com"
                }
            ];

            sessionId = string.Empty;
        }

        logger.LogDebug("Setting {CookieCount} cookies", Cookies?.Length ?? 0);

        await page.SetCookieAsync(Cookies);
    }

    private async Task<CookieParam[]> AuthorizationAsync(IPage page)
    {
        logger.LogInformation("Starting authorization process");

        await page.GoToAsync("https://www.instagram.com/accounts/login/");

        await page.WaitForSelectorAsync("input[name='username']");
        await page.WaitForSelectorAsync("input[name='password']");

        logger.LogDebug("Login page loaded");

        await Task.Delay(random.Next(800, 1000));

        await page.TypeAsync("input[name='username']", login, typeOptions);
        await Task.Delay(random.Next(500, 1000));

        await page.TypeAsync("input[name='password']", password, typeOptions);
        await Task.Delay(random.Next(500, 1000));

        logger.LogInformation("Submitting login form");

        await page.ClickAsync("button[type='submit']");
        await page.WaitForNavigationAsync(navigationOptions);

        Cookies = await page.GetCookiesAsync();

        logger.LogInformation("Authorization successful. Received {CookieCount} cookies", Cookies.Length);

        return Cookies;
    }
}