using SaveFromSocialMediaTgBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SaveFromSocialMediaTgBot.Workers;

public class TelegramBotWorker : BackgroundService
{
    private readonly ILogger<TelegramBotWorker> _logger;
    private readonly TelegramBotClient _client;
    private readonly ScraperService _scraperService;
    private readonly string _botName;

    public TelegramBotWorker(
        ILogger<TelegramBotWorker> logger,
        IConfiguration configuration,
        ScraperService scraperService)
    {
        _botName = configuration.GetValue<string>("BOT_NAME") ?? throw new NullReferenceException();
        var token = configuration.GetValue<string>("TOKEN") ?? throw new NullReferenceException();
        _client = new TelegramBotClient(token);

        _logger = logger;
        _scraperService = scraperService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update is { Type: UpdateType.Message, Message.Type: MessageType.Text })
            {
                var message = update.Message;

                var messageEntities = GetMessageEntities(message);

                var link = messageEntities.FirstOrDefault(x => x.Type == MessageEntityType.Url).Value;
                if (link is null) return;

                _logger.LogInformation($"Received message to bot. ChatId: {message.Chat.Id}. Message : {message.Text}");

                switch (message.Chat.Type)
                {
                    case ChatType.Group:
                    case ChatType.Supergroup:
                    {
                        if (IsBootMention(messageEntities))
                        {
                            await ProcessMessageAsync(botClient, message.Chat.Id, link, cancellationToken);
                        }

                        break;
                    }
                    case ChatType.Private:
                    {
                        await ProcessMessageAsync(botClient, message.Chat.Id, link, cancellationToken);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var message = "я обкакался, вот ошибка " + ex.Message + ex.StackTrace;
            await SendMessageAsync(botClient, update.Message.Chat.Id, message, cancellationToken);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, exception.Message);
        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(ITelegramBotClient botClient, long chatId, string url,
        CancellationToken cancellationToken)
    {
        var videoStream = await _scraperService.GetUrlVideoAsync(url);
        await SendVideoMessageAsync(botClient, chatId, videoStream, cancellationToken);
    }

    private async Task SendMessageAsync(ITelegramBotClient botClient, long chatId, string text,
        CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(chatId: chatId, text: text, cancellationToken: cancellationToken);
    }

    private async Task SendVideoMessageAsync(ITelegramBotClient botClient, long chatId, Stream videoStream,
        CancellationToken cancellationToken)
    {
        await botClient.SendVideo(
            chatId: chatId,
            video: videoStream,
            cancellationToken: cancellationToken);
    }

    private List<(MessageEntityType Type, string Value)> GetMessageEntities(Message message)
    {
        var result = new List<(MessageEntityType, string)>();

        if (message.Entities != null)
        {
            result.AddRange(ParseMessageEntities(message));
        }

        if (message.ReplyToMessage is { Entities: not null })
        {
            result.AddRange(ParseMessageEntities(message.ReplyToMessage));
        }

        return result;
    }

    private List<(MessageEntityType Type, string Value)> ParseMessageEntities(Message message)
    {
        var entityValues = message.EntityValues.ToArray();
        return message.Entities.Select((t, i) => (t.Type, entityValues[i])).ToList();
    }

    private bool IsBootMention(List<(MessageEntityType Type, string Value)> entities)
    {
        return entities.FirstOrDefault(x => x.Type == MessageEntityType.Mention && x.Value == _botName).Value != null;
    }
}