namespace SaveFromSocialMediaTgBot.Abstract.Interface;

public interface IVideoScraper
{
    bool CanHandle(string url);
    Task<Stream> GetVideoStreamAsync(string url);
}