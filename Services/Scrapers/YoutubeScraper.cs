using System.Text.Json;
using System.Text.RegularExpressions;
using Abstract.Data;
using Abstract.Data.Constants;
using Abstract.Interfaces;

namespace Services.Scrapers;

public class YoutubeScraper(HttpClient client) : IScraper
{
    private readonly Regex pattern = new(PatternConstants.Youtube);
    private const string USER_AGENT = "User-Agent";
    private const string USER_AGENT_VALUE = "com.google.ios.youtube/19.45.4 (iPhone16,2; U; CPU iOS 18_1_0 like Mac OS X; US)";

    public bool CanHandle(string url) => url.Contains("youtube.com/shorts", StringComparison.OrdinalIgnoreCase);

    public async Task<ScraperResponse> GetSourceStreamAsync(ScrapedRequest request, CancellationToken ct)
    {
        var url = request.Link;
        var videoUrl = await GetVideoUrlsAsync(url, ct);
        var stream = await client.GetStreamAsync(videoUrl, ct);
        
        return new ScraperResponse([new ScraperResult(stream, MediaType.Video)]);
    }

    private async Task<string> GetVideoUrlsAsync(string url, CancellationToken ct)
    {
        var videoId = GetVideoId(url);
        var visitorData = await GetVisitorData(ct);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/player");

        request.Headers.Add(USER_AGENT, USER_AGENT_VALUE);
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
        request.Headers.Add("Origin", "https://m.youtube.com");
        request.Headers.Add("Referer", "https://m.youtube.com/");
        request.Headers.Add("X-Goog-Visitor-Id", visitorData);
        request.Headers.Add("X-Youtube-Bootstrap-Logged-In", "false");

        request.Content = new StringContent(
            $$"""
              {
                  "videoId": "{{videoId}}",
                  "contentCheckOk": true,
                  "context": {
                      "client": {
                          "clientName": "IOS",
                          "clientVersion": "19.45.4",
                          "deviceMake": "Apple",
                          "deviceModel": "iPhone16,2",
                          "platform": "MOBILE",
                          "osName": "IOS",
                          "osVersion": "18.1.0.22B83",
                          "visitorData": "{{visitorData}}",
                          "hl": "en",
                          "gl": "US",
                          "utcOffsetMinutes": 0
                      }
                  }
              }
              """
        );

        await Task.Delay(new Random().Next(500, 1500), ct);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.GetProperty("playabilityStatus").GetProperty("status").GetString() == "OK")
        {
            return root
                .GetProperty("streamingData")
                .GetProperty("adaptiveFormats")[0]
                .GetProperty("url")
                .GetString()!;
        }

        throw new Exception(MessageConstants.ErrorEmptyUrl);
    }

    private string GetVideoId(string url)
    {
        var result = url.Split('/').Last();
        var questionIndex = result.IndexOf("?", StringComparison.Ordinal);
        return questionIndex != -1
            ? result[..questionIndex]
            : result;
    }

    private async Task<string> GetVisitorData(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.youtube.com/sw.js_data");
        request.Headers.Add(USER_AGENT, USER_AGENT_VALUE);
        request.Headers.Add("Accept", "application/json, text/plain, */*");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

        var response = await client.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        var match = pattern.Match(responseBody);
        if (match.Success)
        {
            return match.Value.Split("\"")[2];
        }

        throw new FormatException(MessageConstants.ErrorEmptyVisitorData);
    }
}