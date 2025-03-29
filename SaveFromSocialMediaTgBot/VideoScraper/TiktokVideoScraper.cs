using System.Net;
using System.Text.RegularExpressions;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class TiktokVideoScraper
{
    private readonly Regex _pattern = new(
        @"https?:\\u002F\\u002F[^""'\s]*?mime_type=video_mp4[^""'\s]*?tt_chain_token",
        RegexOptions.Compiled);

    public async Task<Stream> GetVideoStreamAsync(string pageUrl)
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true
        };
        using var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        var linkVideo = await GetContentLink(httpClient, pageUrl);

        if (linkVideo is null) throw new FormatException("Invalid page URL");

        return await httpClient.GetStreamAsync(linkVideo);
    }

    private async Task<string?> GetContentLink(HttpClient httpClient, string pageUrl)
    {
        string result = null;
        var i = 0;
        while (i < 3 && result is null)
        {
            var response = await httpClient.GetAsync(pageUrl);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();

            var match = _pattern.Match(html);
            if (match.Success)
            {
                result = match.Value.Replace("\\u002F", "/");
            }

            i++;
        }

        if (result != null)
        {
            Console.WriteLine("got link in {0} attemps", i);
        }

        return result;
    }
}