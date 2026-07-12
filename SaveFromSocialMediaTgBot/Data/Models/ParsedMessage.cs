using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SaveFromSocialMediaTgBot.Data.Models;

public class ParsedMessage
{
    private static readonly HashSet<string> ValidCommands =
    [
        "-d", "-description",
        "-u", "-user",
        "-l", "-location",
        "-m", "-music"
    ];

    public int Id { get; }
    public long ChatId { get; }
    public int? ThreadId { get; }
    public long UserId { get; }
    public string? Text { get; }
    public ChatType ChatType { get; }
    public string? BotCommand { get; }
    public bool IsBotMention { get; }
    public ChatSettings Settings { get; }
    public string? Link { get; }
    public Parameters Parameters { get; }

    public MessageEntityType? Type => BotCommand is not null
        ? MessageEntityType.BotCommand
        : Link is not null
            ? MessageEntityType.Url
            : null;

    public ParsedMessage(Message message, string botName, ChatSettings chatSettings)
    {
        Id = message.Id;
        ChatId = message.Chat.Id;
        ThreadId = message.MessageThreadId;
        UserId = message.From!.Id;
        Text = message.Text;
        ChatType = message.Chat.Type;
        Settings = chatSettings;

        var messageEntities = GetMessageEntities(message);
        IsBotMention = CheckIsBotMention(messageEntities, $"@{botName}");
        Link = messageEntities.FirstOrDefault(x => x.Type == MessageEntityType.Url).Value;
        BotCommand = messageEntities.FirstOrDefault(x => x.Type == MessageEntityType.BotCommand).Value;

        var main = SetParameters(message.Text ?? string.Empty);
        var reply = SetParameters(message.ReplyToMessage?.Text ?? string.Empty);
        Parameters = main.Concat(reply)
            .Aggregate(Parameters.None, (current, param)
                => current | param switch
                {
                    "-d" or "-description" => Parameters.Description,
                    "-u" or "-user" => Parameters.User,
                    "-l" or "-location" => Parameters.Location,
                    "-m" or "-music" => Parameters.Music,
                    _ => Parameters.None
                });
    }

    private static List<(MessageEntityType Type, string Value)> GetMessageEntities(Message message)
    {
        var result = new List<(MessageEntityType, string)>();

        if (message.Entities != null)
            result.AddRange(ParseMessageEntities(message));

        if (message.ReplyToMessage is { Entities: not null })
            result.AddRange(ParseMessageEntities(message.ReplyToMessage));

        return result;
    }

    private static List<(MessageEntityType Type, string Value)> ParseMessageEntities(Message message)
    {
        var entityValues = message.EntityValues!.ToArray();
        return message.Entities!.Select((t, i) => (t.Type, entityValues[i])).ToList();
    }

    private static bool CheckIsBotMention(List<(MessageEntityType Type, string Value)> entities, string botName)
    {
        return entities.FirstOrDefault(x => x.Type == MessageEntityType.Mention && x.Value == botName).Value != null;
    }

    private static IEnumerable<string> SetParameters(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        var msg = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return msg.Where(ValidCommands.Contains);
    }
}

[Flags]
public enum Parameters
{
    None = 0,
    Description = 1 << 0,
    User = 1 << 1,
    Location = 1 << 2,
    Music = 1 << 3
}