namespace SaveFromSocialMediaTgBot.Data.Const;

/// <summary>
/// Pattern for searching link video inside
/// </summary>
public static class Pattern
{
    public const string INSTAGRAM = @"""https:\S+?\.mp4\S+?""";
    public const string TICKTOCK = @"https?:\\u002F\\u002F[^""'\s]*?mime_type=video_mp4[^""'\s]*?tt_chain_token";
    public const string TWITTER = @"https?://(?:(?:www|m(?:obile)?)\.)?(?:twitter\.com|x\.com)/(?:(?:i/web|[^/]+)/status|statuses)/(\d+)(?:/(?:video|photo)/(\d+))?";
    public const string YOUTUBE = @"iPhone"",\S+""com.google.ios.youtube/";
}