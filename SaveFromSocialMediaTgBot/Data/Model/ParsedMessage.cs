using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SaveFromSocialMediaTgBot.Data.Model;

public class ParsedMessage
{
    public int Id { get; }
    public long ChatId { get; }
    public long UserId { get; }
    public string? Text { get; }
    public ChatType ChatType { get; }
    public string? BotCommand { get; }
    public bool IsBotMention { get; }
    public ChatSettings Settings { get; }
    public string? VideoLink { get; }

    public ParsedMessage(Message message, string botName, ChatSettings chatSettings)
    {
        Id = message.Id;
        ChatId = message.Chat.Id;
        UserId = message.From.Id;
        Text = message.Text;
        ChatType = message.Chat.Type;
        Settings = chatSettings;

        var messageEntities = GetMessageEntities(message);
        IsBotMention = CheckIsBootMention(messageEntities, botName);
        VideoLink = messageEntities.FirstOrDefault(x => x.Type == MessageEntityType.Url).Value;
        BotCommand = messageEntities.FirstOrDefault(x => x.Type == MessageEntityType.BotCommand).Value;
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
        var entityValues = message.EntityValues.ToArray();
        return message.Entities.Select((t, i) => (t.Type, entityValues[i])).ToList();
    }

    private static bool CheckIsBootMention(List<(MessageEntityType Type, string Value)> entities, string botName)
    {
        return entities.FirstOrDefault(x => x.Type == MessageEntityType.Mention && x.Value == botName).Value != null;
    }
}