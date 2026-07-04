using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Data.Models;
using SaveFromSocialMediaTgBot.Data.Models.Instagram;
using SaveFromSocialMediaTgBot.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using PuppeteerSharp.Input;

namespace SaveFromSocialMediaTgBot.Services.Scraper;

public class InstagramScraper(
    ILogger<InstagramScraper> logger,
    IConfiguration configuration,
    HttpClient client,
    IBrowser browser) : IScraper
{
    private readonly string login = configuration[EnvironmentConstants.InstLogin] ?? "";
    private readonly string password = configuration[EnvironmentConstants.InstPassword] ?? "";
    private string sessionId = configuration[EnvironmentConstants.InstCookieSessionId] ?? "";
    private readonly NavigationOptions navigationOptions = new() { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] };
    private readonly TypeOptions typeOptions = new() { Delay = 150 };
    private readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
    private readonly Random random = new();

    private readonly Regex videoPattern =
        new(PatternConstants.InstagramVideo, RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private readonly Regex carouselPattern =
        new(PatternConstants.InstagramCarousel, RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static CookieParam[]? Cookies { get; set; }

    public bool CanHandle(string url) => url.Contains("instagram.com", StringComparison.OrdinalIgnoreCase);

    public async Task<ScraperResponse> GetSourceStreamAsync(string url, CancellationToken ct) =>
        GetFormatType(url) switch
        {
            FormatType.Reel => new ScraperResponse(await TryGetReelAsync(url)),
            FormatType.Post => new ScraperResponse(await TryGetPostAsync(url)),
            _ => throw new FormatException(MessageConstants.ErrorEmptyUrl)
        };

    private static FormatType GetFormatType(string targetUrl) =>
        targetUrl.Contains("reel", StringComparison.CurrentCultureIgnoreCase)
            ? FormatType.Reel
            : FormatType.Post;


    private async Task<List<ScraperResult>?> TryGetReelAsync(string pageUrl)
    {
        await using var page = await browser.NewPageAsync();

        try
        {
            await SetCookiesAsync(page);

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                logger.LogDebug("Fetching page (attempt {Attempt}) for {Url}", attempt, pageUrl);

                await page.GoToAsync(pageUrl, navigationOptions);
                var content = await page.GetContentAsync();
                content = DecodeContent(content);
                await page.CloseAsync();

                var match = videoPattern.Match(content);
                if (match.Success)
                {
                    logger.LogInformation("Video extracted on attempt {Attempt} for {Url}", attempt, pageUrl);
                    var videoUrl = match.Groups[1].Value;
                    return [await DownloadMediaAsync(videoUrl, MediaType.Video)];
                }

                if (attempt == 1)
                {
                    logger.LogDebug("Video not found, re-authorizing for {Url}", pageUrl);
                    await page.SetCookieAsync(await AuthorizationAsync(page));
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Metadata fetch failed for {Url}", pageUrl);
            throw;
        }
    }

    private async Task<List<ScraperResult>?> TryGetPostAsync(string pageUrl)
    {
        await using var page = await browser.NewPageAsync();

        try
        {
            await SetCookiesAsync(page);

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                logger.LogDebug("Fetching page for {Url}", pageUrl);

                await page.GoToAsync(pageUrl, navigationOptions);
                var content = await page.GetContentAsync();

                content = DecodeContent(content);
                await page.CloseAsync();

                var match = carouselPattern.Matches(content)
                    .FirstOrDefault(x => x.Groups["json"].Value != "[]");

                if (match.Success)
                {
                    var carouselJson = match.Groups["json"].Value;
                    carouselJson = FixInstagramJson(carouselJson).Replace("\r", "\\r").Replace("\n", "\\n");

                    var searchResponse = JsonSerializer.Deserialize<List<SearchResponse>>(carouselJson, options)?
                        .FirstOrDefault();

                    if (searchResponse?.Carousel?.Count > 0)
                    {
                        var collect = new Dictionary<string, ScraperResult>();
                        foreach (var search in searchResponse.Carousel)
                        {
                            var mediaType = search.Video != null ? MediaType.Video : MediaType.Photo;
                            var url = search.Video != null
                                ? search.Video.MaxBy(x => x.Height)?.Url
                                : search.Photos.Items?.MaxBy(x => x.Height)?.Url;

                            collect.TryAdd(search.Id, await DownloadMediaAsync(url, mediaType));
                        }

                        return collect.Select(x => x.Value).ToList();
                    }

                    if (searchResponse?.Video?.Count > 0)
                    {
                        var url = searchResponse.Video.MaxBy(x => x.Height)?.Url;
                        return url != null ? [await DownloadMediaAsync(url, MediaType.Video)] : null;
                    }

                    if (searchResponse?.Image is not null)
                    {
                        var url = searchResponse.Image.Items?.MaxBy(x => x.Height)?.Url;
                        return url != null ? [await DownloadMediaAsync(url, MediaType.Photo)] : null;
                    }
                }

                if (attempt == 1)
                {
                    logger.LogDebug("Video not found, re-authorizing for {Url}", pageUrl);
                    await page.SetCookieAsync(await AuthorizationAsync(page));
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Metadata fetch failed for {Url}", pageUrl);
            throw;
        }
    }

    private async Task SetCookiesAsync(IPage page)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
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

        await page.SetCookieAsync(Cookies);
    }

    private async Task<CookieParam[]> AuthorizationAsync(IPage page)
    {
        logger.LogInformation("Re-authorizing Instagram session");

        await page.GoToAsync("https://www.instagram.com/accounts/login/");
        await page.WaitForSelectorAsync("input[name='username']");
        await page.WaitForSelectorAsync("input[name='password']");
        await Task.Delay(random.Next(800, 1000));
        await page.TypeAsync("input[name='username']", login, typeOptions);
        await Task.Delay(random.Next(500, 1000));
        await page.TypeAsync("input[name='password']", password, typeOptions);
        await Task.Delay(random.Next(500, 1000));
        await page.ClickAsync("button[type='submit']");
        await page.WaitForNavigationAsync(navigationOptions);
        Cookies = await page.GetCookiesAsync();

        logger.LogInformation("Instagram re-authorization successful");

        return Cookies;
    }

    private static string DecodeContent(string rawContent)
    {
        var unescaped = Regex.Unescape(rawContent);
        var fullyDecoded = HttpUtility.HtmlDecode(unescaped);
        fullyDecoded = fullyDecoded.Replace("\\/", "/");
        return fullyDecoded;
    }

    private static string FixInstagramJson(string rawJson)
    {
        var span = rawJson.AsSpan();

        var targetKey = "\"video_dash_manifest\"";
        var keyIndex = span.IndexOf(targetKey);

        if (keyIndex == -1)
            return rawJson;

        var xmlEndTag = "</MPD>";
        var endTagIndex = span[keyIndex..].IndexOf(xmlEndTag);

        if (endTagIndex == -1)
            return rawJson;

        var absoluteEndTagIndex = keyIndex + endTagIndex + xmlEndTag.Length;

        var remainingSpan = span[absoluteEndTagIndex..];

        var closeQuoteIndex = remainingSpan.IndexOf('"');
        if (closeQuoteIndex == -1)
            return rawJson;

        var propertyEndIndex = absoluteEndTagIndex + closeQuoteIndex + 1;

        if (propertyEndIndex < span.Length && span[propertyEndIndex] == ',')
        {
            propertyEndIndex++;
        }

        return string.Concat(span[..keyIndex], span[propertyEndIndex..]);
    }

    private async Task<ScraperResult> DownloadMediaAsync(string url, MediaType type)
    {
        var stream = await client.GetStreamAsync(url);
        return new ScraperResult(stream, type);
    }
}