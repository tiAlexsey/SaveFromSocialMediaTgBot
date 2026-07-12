namespace SaveFromSocialMediaTgBot.Data.Models;

public record ScraperResponse(List<ScraperResult>? Results);

public record ScraperResult(
    Stream Stream,
    MediaType MediaType
)
{
    public string? Text { get; set; } 
}

public enum MediaType
{
    Photo,
    Video
}