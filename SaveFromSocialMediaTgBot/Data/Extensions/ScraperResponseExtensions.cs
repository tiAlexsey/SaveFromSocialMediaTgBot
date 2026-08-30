using SaveFromSocialMediaTgBot.Data.Models;
using Telegram.Bot.Types;

namespace SaveFromSocialMediaTgBot.Data.Extensions;

public static class ScraperResponseExtensions
{
    public static List<IAlbumInputMedia> ToInputMedia(this ScraperResponse? streams, string? caption = null)
    {
        if (streams?.Results is null)
            return [];

        var result = new List<IAlbumInputMedia>();
        for (var i = 0; i < streams.Results.Count; i++)
        {
            var scraperResult = streams.Results[i];
            switch (scraperResult)
            {
                case { MediaType: MediaType.Video }:
                    var video = new InputMediaVideo(
                        InputFile.FromStream(scraperResult.Stream, $"file__{Guid.NewGuid():N}.mp4"));
                    if (i == 0)
                        video.Caption = caption;
                    result.Add(video);
                    break;
                case { MediaType: MediaType.Photo }:
                    var photo = new InputMediaPhoto(
                        InputFile.FromStream(scraperResult.Stream, $"file__{Guid.NewGuid():N}.png"));
                    if (i == 0)
                        photo.Caption = caption;
                    result.Add(photo);
                    break;
            }
        }

        return result.Count > 0 ? result : [];
    }
}