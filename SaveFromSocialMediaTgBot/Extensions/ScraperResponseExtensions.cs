using SaveFromSocialMediaTgBot.Data.Models;
using Telegram.Bot.Types;

namespace SaveFromSocialMediaTgBot.Extensions;

public static class ScraperResponseExtensions
{
    public static List<IAlbumInputMedia> ToInputMedia(this ScraperResponse? streams)
    {
        if (streams?.Results is null)
            return [];

        var result = new List<IAlbumInputMedia>();
        foreach (var scraperResult in streams.Results)
        {
            switch (scraperResult.MediaType)
            {
                case MediaType.Video:
                    var video = new InputMediaVideo(
                        InputFile.FromStream(scraperResult.Stream, $"file__{Guid.NewGuid():N}.mp4"));
                    result.Add(video);
                    break;
                case MediaType.Photo:
                    var photo = new InputMediaPhoto(
                        InputFile.FromStream(scraperResult.Stream, $"file__{Guid.NewGuid():N}.png"));
                    result.Add(photo);
                    break;
            }
        }

        return result.Count > 0 ? result : [];
    }
}