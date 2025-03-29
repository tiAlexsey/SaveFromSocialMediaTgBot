using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SaveFromSocialMediaTgBot.VideoScraper;

public class TwitterVideoScraper
{
    private readonly string _authorization;
    private readonly Regex _twRegex =
        new(
            @"https?://(?:(?:www|m(?:obile)?)\.)?(?:twitter\.com|x\.com)/(?:(?:i/web|[^/]+)/status|statuses)/(\d+)(?:/(?:video|photo)/(\d+))?",
            RegexOptions.Compiled);

    private readonly Dictionary<string, object> variables = new()
    {
        { "with_rux_injections", false },
        { "includePromotedContent", true },
        { "withCommunity", true },
        { "withQuickPromoteEligibilityTweetFields", true },
        { "withBirdwatchNotes", true },
        { "withDownvotePerspective", false },
        { "withVoice", true },
        { "withV2Timeline", true },
        { "withReactionsPerspective", false },
        { "withReactionsMetadata", false },
    };

    private readonly Dictionary<string, object> features = new()
    {
        { "responsive_web_graphql_exclude_directive_enabled", true },
        { "verified_phone_label_enabled", false },
        { "responsive_web_graphql_timeline_navigation_enabled", true },
        { "responsive_web_graphql_skip_user_profile_image_extensions_enabled", false },
        { "tweetypie_unmention_optimization_enabled", true },
        { "vibe_api_enabled", false },
        { "responsive_web_edit_tweet_api_enabled", false },
        { "graphql_is_translatable_rweb_tweet_is_translatable_enabled", false },
        { "view_counts_everywhere_api_enabled", true },
        { "longform_notetweets_consumption_enabled", true },
        { "tweet_awards_web_tipping_enabled", false },
        { "freedom_of_speech_not_reach_fetch_enabled", false },
        { "standardized_nudges_misinfo", false },
        { "tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled", false },
        { "interactive_text_enabled", false },
        { "responsive_web_twitter_blue_verified_badge_is_enabled", true },
        { "responsive_web_text_conversations_enabled", false },
        { "longform_notetweets_richtext_consumption_enabled", false },
        { "responsive_web_enhance_cards_enabled", false },
        { "longform_notetweets_inline_media_enabled", true },
        { "longform_notetweets_rich_text_read_enabled", true },
        { "responsive_web_media_download_video_enabled", true },
        { "responsive_web_twitter_article_tweet_consumption_enabled", true },
        { "creator_subscriptions_tweet_preview_api_enabled", true },
    };

    public TwitterVideoScraper(IConfiguration configuration)
    {
        _authorization = configuration["TWITTER_TOKEN"];
    }

    public async Task<List<string>> GetVideoUrlsAsync(string postId)
    {
        HttpClient client = new();
        await SetCookiesAsync(client);

        variables["tweetId"] = postId;
        var query = JsonSerializer.Serialize(variables);
        var feat = JsonSerializer.Serialize(features);
        var url =
            $"https://x.com/i/api/graphql/2ICDjqPd81tulZcYrtpTuQ/TweetResultByRestId?variables={Uri.EscapeDataString(query)}&features={Uri.EscapeDataString(feat)}";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var media = json.RootElement.GetProperty("data").GetProperty("tweetResult").GetProperty("result")
            .GetProperty("legacy").GetProperty("entities").GetProperty("media");
        var videoUrls = new List<string>();
        foreach (var item in media.EnumerateArray())
        {
            if (item.TryGetProperty("video_info", out var videoInfo))
            {
                var variants = videoInfo.GetProperty("variants").EnumerateArray();
                var best = variants.Where(v => v.TryGetProperty("bitrate", out _))
                    .OrderByDescending(v => v.GetProperty("bitrate").GetInt32())
                    .First();
                videoUrls.Add(best.GetProperty("url").GetString());
            }
        }

        return videoUrls;
    }

    public string GetPostId(string url)
    {
        var match = _twRegex.Match(url);
        if (!match.Success)
            throw new ArgumentException("Invalid Twitter URL.");
        return match.Groups[1].Value;
    }

    private async Task SetCookiesAsync(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authorization);
        var response = await client.PostAsync("https://api.x.com/1.1/guest/activate.json", null);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("guest_token").GetString();
        client.DefaultRequestHeaders.Add("x-guest-token", token);
    }
}