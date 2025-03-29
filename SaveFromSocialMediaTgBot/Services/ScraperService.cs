using SaveFromSocialMediaTgBot.VideoScraper;

namespace SaveFromSocialMediaTgBot.Services;

public class ScraperService
{
    private readonly InstagramVideoScraper _instagramScraper;
    private readonly TiktokVideoScraper _tiktokScraper;
    private readonly TwitterVideoScraper _twitterScraper;

    public ScraperService(
        InstagramVideoScraper instagramScraper,
        TiktokVideoScraper tiktokScraper,
        TwitterVideoScraper twitterScraper)
    {
        _instagramScraper = instagramScraper;
        _tiktokScraper = tiktokScraper;
        _twitterScraper = twitterScraper;
    }

    public async Task<Stream> GetUrlVideoAsync(string url)
    {
        return url switch
        {
            _ when url.Contains("instagram") => await GetVideoFromInstAsync(url),
            _ when url.Contains("twitter") || url.Contains("x.com") => await GetVideoFromTwitterAsync(url),
            _ when url.Contains("tiktok") => await GetVideoFromTiktokAsync(url),
            _ => throw new NullReferenceException()
        };
    }

    private async Task<Stream> GetVideoFromInstAsync(string url)
    {
        return await _instagramScraper.GetVideoStreamAsync(url);
    }

    private async Task<Stream> GetVideoFromTiktokAsync(string url)
    {
        return await _tiktokScraper.GetVideoStreamAsync(url);
    }

    private async Task<Stream> GetVideoFromTwitterAsync(string url)
    {
        var postId = _twitterScraper.GetPostId(url);
        var videoUrl = (await _twitterScraper.GetVideoUrlsAsync(postId)).FirstOrDefault();
        HttpClient httpClient = new();
        return await httpClient.GetStreamAsync(videoUrl);
    }
}