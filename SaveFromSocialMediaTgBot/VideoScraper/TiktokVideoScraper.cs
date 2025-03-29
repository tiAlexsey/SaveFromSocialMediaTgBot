using System.Net;
using System.Text.RegularExpressions;
using SaveFromSocialMediaTgBot.Data.Const;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class TiktokVideoScraper
{
    private readonly int _retryCount;
    private readonly Regex _pattern = new(
        @"https?:\\u002F\\u002F[^""'\s]*?mime_type=video_mp4[^""'\s]*?tt_chain_token",
        RegexOptions.Compiled);

    public TiktokVideoScraper(IConfiguration configuration)
    {
        _retryCount = int.TryParse(configuration["RETRY_COUNT"], out var retryCount) ? retryCount : 1;
    }
    
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

        var linkVideo = await GetVideoLinkAsync(httpClient, pageUrl);

        if (linkVideo is null) throw new FormatException(Messages.ERROR_EMPTY_URL);

        return await httpClient.GetStreamAsync(linkVideo);
    }

    private async Task<string?> GetVideoLinkAsync(HttpClient httpClient, string pageUrl)
    {
        string result = null;
        var i = 0;
        while (i < _retryCount && result is null)
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