using SaveFromSocialMediaTgBot.Data.Models;
using SaveFromSocialMediaTgBot.Exceptions;
using SaveFromSocialMediaTgBot.Interfaces;

namespace SaveFromSocialMediaTgBot.Services;

public class ScraperService(IEnumerable<IScraper> scrapers)
{
    public async Task<ScraperResponse> GetSourceStreamAsync(string url, CancellationToken ct)
    {
        var scrapper = scrapers.FirstOrDefault(x => x.CanHandle(url)) ?? throw new InvalidUrlException();
        return await scrapper.GetSourceStreamAsync(url, ct);
    }
}