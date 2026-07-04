using SaveFromSocialMediaTgBot.Data.Models;

namespace SaveFromSocialMediaTgBot.Interfaces;

public interface IScraper
{
    bool CanHandle(string url);
    Task<ScraperResponse> GetSourceStreamAsync(string url, CancellationToken ct = default);
}