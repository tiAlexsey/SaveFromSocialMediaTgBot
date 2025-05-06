using SaveFromSocialMediaTgBot.Abstract.Interface;

namespace SaveFromSocialMediaTgBot.Services;

public class ScraperService(IEnumerable<IVideoScraper> videoScrapers)
{
    public async Task<Stream> GetVideoStreamAsync(string url)
    {
        var scrapper = videoScrapers.FirstOrDefault(x => x.CanHandle(url)) ?? throw new InvalidOperationException();
        return await scrapper.GetVideoStreamAsync(url);
    }
}