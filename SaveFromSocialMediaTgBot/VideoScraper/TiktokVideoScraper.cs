using System.Text.RegularExpressions;
using SaveFromSocialMediaTgBot.Abstract.Interface;
using SaveFromSocialMediaTgBot.Data.Const;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class TiktokVideoScraper(IConfiguration configuration, HttpClient client) : IVideoScraper
{
    private readonly int retryCount = int.TryParse(configuration[Env.RETRY_COUNT], out var count) ? count : 1;
    private readonly Regex pattern = new(Pattern.TICKTOCK, RegexOptions.Compiled);


    public bool CanHandle(string url)
        => url.Contains("tiktok", StringComparison.OrdinalIgnoreCase);

    public async Task<Stream> GetVideoStreamAsync(string pageUrl)
    {
        var linkVideo = await GetVideoLinkAsync(client, pageUrl);

        if (linkVideo is null) throw new FormatException(Messages.ERROR_EMPTY_URL);

        return await client.GetStreamAsync(linkVideo);
    }

    private async Task<string?> GetVideoLinkAsync(HttpClient httpClient, string pageUrl)
    {
        string? result = null;
        var i = 0;
        while (i < retryCount && result is null)
        {
            var response = await httpClient.GetAsync(pageUrl);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();

            var match = pattern.Match(html);
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