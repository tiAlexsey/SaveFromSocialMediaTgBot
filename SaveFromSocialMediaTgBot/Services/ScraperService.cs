using SaveFromSocialMediaTgBot.VideoScraper;

namespace SaveFromSocialMediaTgBot.Services;

public class ScraperService(
    InstagramVideoScraper instagramScraper,
    TiktokVideoScraper tiktokScraper,
    TwitterVideoScraper twitterScraper,
    YoutubeVideoScraper youtubeScraper)
{
    public async Task<Stream> GetUrlVideoAsync(string url)
    {
        return url switch
        {
            _ when url.Contains("instagram") => await GetVideoFromInstAsync(url),
            _ when url.Contains("twitter") || url.Contains("x.com") => await GetVideoFromTwitterAsync(url),
            _ when url.Contains("tiktok") => await GetVideoFromTiktokAsync(url),
            _ when url.Contains("youtube.com/shorts") => await GetVideoFromYoutubeAsync(url),
            _ => throw new NullReferenceException()
        };
    }

    private async Task<Stream> GetVideoFromInstAsync(string url)
    {
        var videoUrl = await instagramScraper.GetVideoUrlAsync(url);
        HttpClient client = new();
        return await client.GetStreamAsync(videoUrl);
    }

    private async Task<Stream> GetVideoFromTiktokAsync(string url)
    {
        return await tiktokScraper.GetVideoStreamAsync(url);
    }

    private async Task<Stream> GetVideoFromTwitterAsync(string url)
    {
        var postId = twitterScraper.GetPostId(url);
        var videoUrl = (await twitterScraper.GetVideoUrlsAsync(postId)).FirstOrDefault();
        HttpClient httpClient = new();
        return await httpClient.GetStreamAsync(videoUrl);
    }

    private async Task<Stream> GetVideoFromYoutubeAsync(string url)
    {
        var videoUrl = await youtubeScraper.GetVideoUrlsAsync(url);
        HttpClient httpClient = new();
        return await httpClient.GetStreamAsync(videoUrl);
    }
}