using System.Text;
using PuppeteerSharp;
using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Data.Models;
using SaveFromSocialMediaTgBot.Data.Models.Instagram;
using SaveFromSocialMediaTgBot.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using PuppeteerSharp.Input;
using SaveFromSocialMediaTgBot.Extensions;

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

    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
    };

    private readonly Random random = new();

    private readonly Regex reelPattern =
        new(PatternConstants.InstagramReel, RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private readonly Regex postPattern =
        new(PatternConstants.InstagramPost, RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static CookieParam[]? Cookies { get; set; }

    public bool CanHandle(string url) => url.Contains("instagram.com", StringComparison.OrdinalIgnoreCase);

    public async Task<ScraperResponse> GetSourceStreamAsync(ScrapedRequest request, CancellationToken ct)
    {
        logger.LogInformation("Start processing {Url}", request.Link);

        var format = GetFormatType(request.Link);
        var results = format switch
        {
            FormatType.Reel => await ExtractDataAsync(request, TryParseReel),
            FormatType.Post => await ExtractDataAsync(request, TryParsePost),
            _ => throw new FormatException(MessageConstants.ErrorEmptyUrl)
        };

        return new ScraperResponse(results);
    }

    private static FormatType GetFormatType(string targetUrl) =>
        targetUrl.Contains("reel", StringComparison.CurrentCultureIgnoreCase)
            ? FormatType.Reel
            : FormatType.Post;

    private async Task<List<ScraperResult>?> ExtractDataAsync(
        ScrapedRequest request,
        Func<string, ScrapedRequest, Task<List<ScraperResult>?>> parseFunc)
    {
        await using var page = await browser.NewPageAsync();
        var url = request.Link;

        try
        {
            await SetCookiesAsync(page);

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                logger.LogDebug("Fetching page (attempt {Attempt}) for {Url}", attempt, url);

                await page.GoToAsync(url, navigationOptions);
                var content = await page.GetContentAsync();
                content = DecodeContent(content);

                var result = await parseFunc(content, request);
                if (result != null)
                    return result;

                if (attempt == 1)
                {
                    logger.LogDebug("Content not found, re-authorizing for {Url}", url);
                    await page.SetCookieAsync(await AuthorizationAsync(page));
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Metadata fetch failed for {Url}", url);
            throw;
        }
    }

    private async Task<List<ScraperResult>?> TryParseReel(string content, ScrapedRequest request)
    {
        var match = reelPattern.Match(content);
        if (!match.Success)
            return null;

        if (!JsonCorrection.TryCorrectJson(match.Value, out var reelJson))
            return null;

        var reel = JsonSerializer.Deserialize<SearchResponse>(reelJson, options);

        if (reel?.Video == null)
            return null;

        var result = await DownloadMediaAsync(reel.Video.FirstOrDefault()?.Url, MediaType.Video);
        result.Text = MapParams(request, reel);
        return [result];
    }

    private async Task<List<ScraperResult>?> TryParsePost(string content, ScrapedRequest request)
    {
        var match = postPattern.Matches(content)
            .FirstOrDefault(x => x.Groups["json"].Value != "[]");

        if (match is not { Success: true })
            return null;

        var postJson = match.Groups["json"].Value;
        if (!JsonCorrection.TryCorrectJson(postJson, out postJson))
            return null;

        var searchResponse = JsonSerializer.Deserialize<List<SearchResponse>>(postJson, options)?
            .FirstOrDefault();

        if (searchResponse == null)
            return null;

        if (searchResponse.Carousel?.Count > 0)
        {
            var collect = new Dictionary<string, ScraperResult>();
            foreach (var search in searchResponse.Carousel)
            {
                var mediaType = search.Video != null ? MediaType.Video : MediaType.Photo;
                var mediaUrl = search.Video != null
                    ? search.Video.MaxBy(x => x.Height)?.Url
                    : search.Photos?.Items?.MaxBy(x => x.Height)?.Url;

                if (mediaUrl == null) continue;

                var result = await DownloadMediaAsync(mediaUrl, mediaType);
                result.Text = MapParams(request, searchResponse);
                collect.TryAdd(search.Id, result);
            }

            return collect.Values.ToList();
        }

        string? url = null;
        var type = MediaType.Photo;

        if (searchResponse.Video?.Count > 0)
        {
            url = searchResponse.Video.MaxBy(x => x.Height)?.Url;
            type = MediaType.Video;
        }
        else if (searchResponse.Image?.Items != null)
        {
            url = searchResponse.Image.Items.MaxBy(x => x.Height)?.Url;
            type = MediaType.Photo;
        }

        if (url == null)
            return null;

        var singleResult = await DownloadMediaAsync(url, type);
        singleResult.Text = MapParams(request, searchResponse);
        return [singleResult];
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
        return fullyDecoded.Replace("\\/", "/");
    }

    private async Task<ScraperResult> DownloadMediaAsync(string? url, MediaType type)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));

        var stream = await client.GetStreamAsync(url);
        return new ScraperResult(stream, type);
    }

    private static string MapParams(ScrapedRequest request, SearchResponse searchResponse)
    {
        var requested = request.Parameters;
        if (requested == Parameters.None)
            return string.Empty;

        var sb = new StringBuilder();

        if (requested.HasFlag(Parameters.Description) && searchResponse.Caption?.Text != null)
            sb.AppendLine(searchResponse.Caption.Text).AppendLine();

        if (requested.HasFlag(Parameters.User) && searchResponse.User != null)
            sb.AppendLine($"User: https://www.instagram.com/{searchResponse.User.Name}/");

        if (requested.HasFlag(Parameters.Location) && searchResponse.Location?.Name != null)
            sb.AppendLine($"Location: {searchResponse.Location.Name}");

        if (requested.HasFlag(Parameters.Music) && searchResponse.Metadata?.MusicInfo?.Asset != null)
        {
            var asset = searchResponse.Metadata.MusicInfo.Asset;
            sb.AppendLine($"Music: {asset.Artist} - {asset.Title}");
        }

        return sb.ToString().TrimEnd();
    }
}