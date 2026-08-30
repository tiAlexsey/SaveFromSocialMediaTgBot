using System.Text.Json.Serialization;

namespace Abstract.Data.Instagram;

public record SearchResponse(
    [property: JsonPropertyName("carousel_media")]
    List<Carousel>? Carousel,
    [property: JsonPropertyName("caption")]
    Caption? Caption,
    [property: JsonPropertyName("video_versions")]
    List<Item>? Video,
    [property: JsonPropertyName("image_versions2")]
    Image? Image,
    [property: JsonPropertyName("user")]
    User? User,
    [property: JsonPropertyName("clips_metadata")]
    Metadata? Metadata,
    [property: JsonPropertyName("location")]
    Location? Location
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

public record User(
    [property: JsonPropertyName("username")]
    string Name
);

public record Metadata(
    [property: JsonPropertyName("music_info")]
    MusicInfo MusicInfo
);

public record MusicInfo(
    [property: JsonPropertyName("music_asset_info")]
    MusicAsset? Asset
);

public record MusicAsset(
    [property: JsonPropertyName("display_artist")]
    string Artist,
    [property: JsonPropertyName("title")]
    string Title
);

public record Location(
    [property: JsonPropertyName("name")]
    string Name
);