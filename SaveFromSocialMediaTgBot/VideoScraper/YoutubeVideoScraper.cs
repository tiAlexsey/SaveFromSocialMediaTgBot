using System.Text.Json;
using System.Text.RegularExpressions;
using SaveFromSocialMediaTgBot.Data.Const;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class YoutubeVideoScraper
{
    private readonly Regex _pattern = new(@"iPhone"",\S+""com.google.ios.youtube/");
    private const string USER_AGENT = "User-Agent";
    private const string USER_AGENT_VALUE = "com.google.ios.youtube/19.45.4 (iPhone16,2; U; CPU iOS 18_1_0 like Mac OS X; US)";

    public async Task<string> GetVideoUrlsAsync(string url)
    {
        var videoId = GetVideoId(url);
        var visitorData = await GetVisitorData();
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/player");
        request.Headers.Add(USER_AGENT, USER_AGENT_VALUE);
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
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        return root
            .GetProperty("streamingData")
            .GetProperty("adaptiveFormats")[0]
            .GetProperty("url")
            .GetString();
    }

    private string GetVideoId(string url)
    {
        var result = url.Split('/').Last();
        var questionIndex = result.IndexOf("?", StringComparison.Ordinal);
        return questionIndex != -1
            ? result[..questionIndex]
            : result;
    }

    private async Task<string> GetVisitorData()
    {
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.youtube.com/sw.js_data");
        request.Headers.Add(USER_AGENT, USER_AGENT_VALUE);
        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        var match = _pattern.Match(responseBody);
        if (match.Success)
        {
            return match.Value.Split("\"")[2];
        }

        throw new FormatException(Messages.ERROR_EMPTY_VISITOR_DATA);
    }
}