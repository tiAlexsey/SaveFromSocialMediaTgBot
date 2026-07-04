namespace SaveFromSocialMediaTgBot.Data.Models;

public record ScraperResponse(List<ScraperResult>? Results);

public record ScraperResult(Stream Stream, MediaType MediaType);

public enum MediaType
{
    Photo,
    Video
}