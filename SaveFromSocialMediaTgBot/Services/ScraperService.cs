using SaveFromSocialMediaTgBot.Abstract.Interface;

namespace SaveFromSocialMediaTgBot.Services;

public class ScraperService(
    IInstagramVideoScraper instagramScraper,
    ITiktokVideoScraper tiktokScraper,
    ITwitterVideoScraper twitterScraper,
    IYoutubeVideoScraper youtubeScraper) : IVideoScraper
{
    public async Task<Stream> GetVideoStreamAsync(string url)
    {
        return url switch
        {
            _ when url.Contains("instagram") => await instagramScraper.GetVideoStreamAsync(url),
            _ when url.Contains("twitter") || url.Contains("x.com") => await twitterScraper.GetVideoStreamAsync(url),
            _ when url.Contains("tiktok") => await tiktokScraper.GetVideoStreamAsync(url),
            _ when url.Contains("youtube.com/shorts") => await youtubeScraper.GetVideoStreamAsync(url),
            _ => throw new NullReferenceException()
        };
    }
}