namespace SaveFromSocialMediaTgBot.Abstract.Interface;

public interface IVideoScraper
{
    Task<Stream> GetVideoStreamAsync(string url);
}