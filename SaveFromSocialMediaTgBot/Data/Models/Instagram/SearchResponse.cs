using System.Text.Json.Serialization;

namespace SaveFromSocialMediaTgBot.Data.Models.Instagram;

public record SearchResponse(
    [property: JsonPropertyName("carousel_media")]
    List<Carousel>? Carousel,
    [property: JsonPropertyName("caption")]
    Caption? Caption,
    [property: JsonPropertyName("video_versions")]
    List<Item>? Video,
    [property: JsonPropertyName("image_versions2")]
    Image? Image
);

public record Carousel(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("image_versions2")]
    Image Photos,
    [property: JsonPropertyName("video_versions")]
    List<Item>? Video
);

public record Image(
    [property: JsonPropertyName("candidates")]
    List<Item>? Items
);

public record Item(
    [property: JsonPropertyName("url")]
    string Url,
    [property: JsonPropertyName("height")]
    int Height,
    [property: JsonPropertyName("width")]
    int Width
);

public record Caption(
    [property: JsonPropertyName("text")]
    string Text
);