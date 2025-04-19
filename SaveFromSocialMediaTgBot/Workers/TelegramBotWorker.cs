using SaveFromSocialMediaTgBot.Abstract.Interface;
using SaveFromSocialMediaTgBot.Data.Const;
using SaveFromSocialMediaTgBot.Data.Model;
using SaveFromSocialMediaTgBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SaveFromSocialMediaTgBot.Workers;

public class TelegramBotWorker : BackgroundService
{
    private readonly ILogger<TelegramBotWorker> _logger;
    private readonly TelegramBotClient _client;
    private readonly ScraperService _scraperService;
    private readonly string _botName;
    private readonly ICacheService _cacheService;

    public TelegramBotWorker(
        ILogger<TelegramBotWorker> logger,
        IConfiguration configuration,
        ScraperService scraperService,
        ICacheService cacheService)
    {
        _botName = configuration.GetValue<string>("BOT_NAME") ?? throw new NullReferenceException();
        var token = configuration.GetValue<string>("TOKEN") ?? throw new NullReferenceException();
        _client = new TelegramBotClient(token);

        _logger = logger;
        _scraperService = scraperService;
        _cacheService = cacheService;
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
        if (update.CallbackQuery is not null)
        {
            await CallbackQueryHandlerAsync(botClient, update, cancellationToken);
            await botClient.DeleteMessage(new ChatId(update.CallbackQuery.Message.Chat.Id),
                update.CallbackQuery.Message.MessageId, cancellationToken);
            return;
        }

        if (update is not { Type: UpdateType.Message, Message.Type: MessageType.Text })
        {
            return;
        }

        try
        {
            var chatSettings = await _cacheService.GetOrCreateAsync(
                update.Message.Chat.Id.ToString(), async () => new ChatSettings());
            var message = new ParsedMessage(update.Message, _botName, chatSettings);

            if (message.BotCommand is not null)
            {
                await SettingHandlerAsync(botClient, message, cancellationToken);
                return;
            }

            if (message.VideoLink is not null)
            {
                await LinkHandlerAsync(botClient, message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var logMessage = $"я обкакался, вот ошибка: {ex.Message}";
            _logger.LogError(logMessage + "\n" + ex.Message + ex);
            await botClient.SetMessageReaction(update.Message.Chat.Id, update.Message.Id, ["\ud83d\udca9"],
                cancellationToken: cancellationToken);
        }
    }

    private async Task StartBotWorkflowAsync(ITelegramBotClient botClient, ParsedMessage message,
        CancellationToken cancellationToken)
    {
        await botClient.SetMessageReaction(message.ChatId, message.Id, ["\ud83d\udc40"],
            cancellationToken: cancellationToken);
        var videoStream = await _scraperService.GetUrlVideoAsync(message.VideoLink);
        await botClient.SendVideo(chatId: message.ChatId, video: videoStream, cancellationToken: cancellationToken);
        await botClient.SetMessageReaction(message.ChatId, message.Id, ["\ud83d\udcaf"],
            cancellationToken: cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, exception.Message);
        return Task.CompletedTask;
    }

    private async Task SettingHandlerAsync(ITelegramBotClient botClient, ParsedMessage message,
        CancellationToken cancellationToken)
    {
        if (message.BotCommand is null)
            return;

        if (!await IsAllowSettingsAsync(botClient, message.ChatId, message.UserId, cancellationToken))
        {
            await botClient.DeleteMessage(message.ChatId, message.Id, cancellationToken);
            return;
        }

        switch (message.BotCommand)
        {
            case var x when x == $"{Command.NO_MENTION_MODE_COMMAND}{_botName}":
                var keyboard = new InlineKeyboardMarkup([
                    [
                        InlineKeyboardButton.WithCallbackData(Button.TURN_ON, true.ToString()),
                        InlineKeyboardButton.WithCallbackData(Button.TURN_OFF, false.ToString())
                    ]
                ]);
                await botClient.SendTextMessageAsync(
                    chatId: message.ChatId,
                    text: Command.NO_MENTION_MODE_TEXT,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
                await botClient.DeleteMessage(chatId: message.ChatId, messageId: message.Id,
                    cancellationToken: cancellationToken);
                break;
            default:
                break;
        }
    }

    private static async Task<bool> IsAllowSettingsAsync(ITelegramBotClient botClient, long chatId, long userId,
        CancellationToken cancellationToken)
    {
        var member = await botClient.GetChatMember(chatId, userId, cancellationToken);
        return member.Status is ChatMemberStatus.Creator or ChatMemberStatus.Administrator;
    }

    private async Task LinkHandlerAsync(ITelegramBotClient botClient, ParsedMessage message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Received message to bot. ChatId: {message.ChatId}. Message : {message.Text}");

        switch (message.ChatType)
        {
            case ChatType.Group:
            case ChatType.Supergroup:
            {
                if (message.Settings.NeedMention && !message.IsBotMention)
                    break;

                await StartBotWorkflowAsync(botClient, message, cancellationToken);
                break;
            }
            case ChatType.Private:
            {
                await StartBotWorkflowAsync(botClient, message, cancellationToken);
                break;
            }
            default:
                return;
        }
    }

    private async Task CallbackQueryHandlerAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var model = new
        {
            ChatId = update.CallbackQuery.Message.Chat.Id,
            UserId = update.CallbackQuery.From.Id,
            Command = update.CallbackQuery.Message?.Text,
            Value = update.CallbackQuery.Data,
        };

        if (!await IsAllowSettingsAsync(botClient, model.ChatId, model.UserId, cancellationToken))
        {
            await botClient.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id, text: Messages.ACCESS_DENIED);
            return;
        }

        var chatSettings = await _cacheService.GetOrCreateAsync(model.ChatId.ToString(),
            async () => new ChatSettings());

        switch (model.Command)
        {
            case Command.NO_MENTION_MODE_TEXT:
            {
                if (bool.TryParse(model.Value, out var result))
                {
                    chatSettings.NeedMention = result;
                }

                await _cacheService.SetAsync(model.ChatId.ToString(), chatSettings);
                await botClient.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id, text: Messages.SUCCESS);
                break;
            }
        }
    }
}