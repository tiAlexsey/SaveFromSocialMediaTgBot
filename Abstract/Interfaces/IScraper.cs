using Abstract.Data;

namespace Abstract.Interfaces;

public interface IScraper
{
    bool CanHandle(string url);
    Task<ScraperResponse> GetSourceStreamAsync(ScrapedRequest request, CancellationToken ct = default);
}