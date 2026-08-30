using Abstract.Data;
using Abstract.Data.Exceptions;
using Abstract.Interfaces;

namespace Services;

public class ScraperService(IEnumerable<IScraper> scrapers)
{
    public async Task<ScraperResponse> GetSourceStreamAsync(ScrapedRequest request, CancellationToken ct)
    {
        var scrapper = scrapers.FirstOrDefault(x => x.CanHandle(request.Link)) ?? throw new InvalidUrlException();
        return await scrapper.GetSourceStreamAsync(request, ct);
    }
}