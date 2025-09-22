namespace SaveFromSocialMediaTgBot.Data.Models;

public class ChatSettings
{
    public bool Mention { get; set; } = true;
    public bool Notification { get; set; } = true;
    public bool DeleteOriginMessage { get; set; }
}