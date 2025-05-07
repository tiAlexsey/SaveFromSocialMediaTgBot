using SaveFromSocialMediaTgBot.Abstract.Interface;
using SaveFromSocialMediaTgBot.Data.Const;
using SaveFromSocialMediaTgBot.Data.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SaveFromSocialMediaTgBot.Services;

public class TelegramBotService(
    ScraperService scraperService,
    ICacheService cacheService,
    ILogger<TelegramBotService> logger,
    IConfiguration configuration) : ITelegramBotService
{
    private readonly string botName = configuration[Env.BOT_NAME] ?? throw new NullReferenceException();

    public async Task UpdateMessageWorkflowAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var chatSettings = await cacheService.GetOrCreateAsync(update.Message!.Chat.Id.ToString(),
            async () => new ChatSettings());
        var message = new ParsedMessage(update.Message, botName, chatSettings);

        switch (message.Type)
        {
            case MessageEntityType.BotCommand:
                await ProcessBotCommandAsync(botClient, message, cancellationToken);
                return;
            case MessageEntityType.Url:
                await LinkHandlerAsync(botClient, message, cancellationToken);
                break;
            default:
                return;
        }
    }

    public async Task UpdateCallbackWorkflowAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        await ProcessCallbackAsync(botClient, update, cancellationToken);
        await botClient.DeleteMessage(new ChatId(update.CallbackQuery!.Message!.Chat.Id),
            update.CallbackQuery.Message.MessageId, cancellationToken);
    }

    private async Task ProcessLinkMessageAsync(ITelegramBotClient botClient, ParsedMessage message,
        CancellationToken cancellationToken)
    {
        await botClient.SetMessageReaction(message.ChatId, message.Id, ["\ud83d\udc40"],
            cancellationToken: cancellationToken);
        var videoStream = await scraperService.GetVideoStreamAsync(message.VideoLink!);
        await botClient.SendVideo(chatId: message.ChatId, video: videoStream, cancellationToken: cancellationToken);
        await botClient.SetMessageReaction(message.ChatId, message.Id, ["\ud83d\udcaf"],
            cancellationToken: cancellationToken);
    }

    private async Task ProcessBotCommandAsync(ITelegramBotClient botClient, ParsedMessage message,
        CancellationToken cancellationToken)
    {
        if (!await IsAllowSettingsAsync(botClient, message.ChatId, message.UserId, cancellationToken))
        {
            await botClient.DeleteMessage(message.ChatId, message.Id, cancellationToken);
            return;
        }

        switch (message.BotCommand)
        {
            case var x when x == $"{Command.NO_MENTION_MODE_COMMAND}{botName}":
                var keyboard = new InlineKeyboardMarkup([
                    [
                        InlineKeyboardButton.WithCallbackData(Button.TURN_ON, true.ToString()),
                        InlineKeyboardButton.WithCallbackData(Button.TURN_OFF, false.ToString())
                    ]
                ]);
                await botClient.SendMessage(
                    chatId: message.ChatId,
                    text: Command.NO_MENTION_MODE_TEXT,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
                await botClient.DeleteMessage(chatId: message.ChatId, messageId: message.Id,
                    cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task ProcessCallbackAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var model = new
        {
            ChatId = update.CallbackQuery!.Message!.Chat.Id,
            UserId = update.CallbackQuery.From.Id,
            Command = update.CallbackQuery.Message?.Text,
            Value = update.CallbackQuery.Data,
        };

        if (!await IsAllowSettingsAsync(botClient, model.ChatId, model.UserId, cancellationToken))
        {
            await botClient.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id, text: Messages.ACCESS_DENIED,
                cancellationToken: cancellationToken);
            return;
        }

        var chatSettings = await cacheService.GetOrCreateAsync(model.ChatId.ToString(),
            async () => new ChatSettings());

        switch (model.Command)
        {
            case Command.NO_MENTION_MODE_TEXT:
            {
                if (bool.TryParse(model.Value, out var result))
                {
                    chatSettings.NeedMention = result;
                }

                await cacheService.SetAsync(model.ChatId.ToString(), chatSettings);
                await botClient.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id, text: Messages.SUCCESS,
                    cancellationToken: cancellationToken);
                break;
            }
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
        logger.LogInformation($"Received message to bot. ChatId: {message.ChatId}. Message : {message.Text}");

        switch (message.ChatType)
        {
            case ChatType.Group:
            case ChatType.Supergroup:
            {
                if (message.Settings.NeedMention && !message.IsBotMention)
                    break;

                await ProcessLinkMessageAsync(botClient, message, cancellationToken);
                break;
            }
            case ChatType.Private:
            {
                await ProcessLinkMessageAsync(botClient, message, cancellationToken);
                break;
            }
            default:
                return;
        }
    }
}