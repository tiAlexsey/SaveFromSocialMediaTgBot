using System.Text.RegularExpressions;
using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Data.Models;
using SaveFromSocialMediaTgBot.Interfaces;

namespace SaveFromSocialMediaTgBot.Services.Scraper;

public class TiktokScraper(ILogger<TiktokScraper> logger, IConfiguration configuration, HttpClient client) : IScraper
{
    private readonly int retryCount =
        int.TryParse(configuration[EnvironmentConstants.RetryCount], out var count) ? count : 1;

    private readonly Regex pattern = new(PatternConstants.TickTock, RegexOptions.Compiled);

    public bool CanHandle(string url) => url.Contains("tiktok", StringComparison.OrdinalIgnoreCase);

    public async Task<ScraperResponse> GetSourceStreamAsync(string url, CancellationToken ct)
    {
        logger.LogInformation("Start processing {Url}", url);

        var videoUrl = await GetVideoLinkAsync(client, url, ct) ??
                       throw new FormatException(MessageConstants.ErrorEmptyUrl);

        logger.LogInformation("Video URL resolved for {Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Get, videoUrl) { Headers = { Referrer = new Uri(url) } };

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);

        logger.LogInformation("Stream opened successfully for {Url}", url);

        return new ScraperResponse([new ScraperResult(stream, MediaType.Video)]);
    }

    private async Task<string?> GetVideoLinkAsync(HttpClient httpClient, string url, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            logger.LogDebug("Fetching metadata (attempt {Attempt}) for {Url}", attempt, url);

            var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);

            var match = pattern.Match(content);
            if (match.Success)
            {
                logger.LogInformation("Video extracted on attempt {Attempt} for {Url}", attempt, url);

                return match.Value.Replace("\\u002F", "/");
            }
        }

        return null;
    }
}