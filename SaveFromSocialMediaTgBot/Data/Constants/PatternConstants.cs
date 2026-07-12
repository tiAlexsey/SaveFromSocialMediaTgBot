namespace SaveFromSocialMediaTgBot.Data.Constants;

/// <summary>
/// Pattern for searching video link inside html
/// </summary>
public static class PatternConstants
{
    public const string InstagramReel = @"(?<=""media"":).*?(?=,""__typename"":""XDTClipsItemDict""},""cursor"")";
    public const string InstagramPost = @"""items""\s*:\s*(?<json>\[(?:[^\[\]]|(?<open>\[)|(?<-open>\]))*(?(open)(?!))\])";
    public const string TickTock = @"https?:\\u002F\\u002F[^""'\s]*?mime_type=video_mp4[^""'\s]*?tt_chain_token";
    public const string TwitterVideo = @"https?://(?:(?:www|m(?:obile)?)\.)?(?:twitter\.com|x\.com)/(?:(?:i/web|[^/]+)/status|statuses)/(\d+)(?:/(?:video|photo)/(\d+))?";
    public const string Youtube = @"iPhone"",\S+""com.google.ios.youtube/";
}