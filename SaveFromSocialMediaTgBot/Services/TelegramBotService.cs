using SaveFromSocialMediaTgBot.Data.Constants;
using SaveFromSocialMediaTgBot.Data.Models;
using SaveFromSocialMediaTgBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SaveFromSocialMediaTgBot.Services;

public class TelegramBotService(
    ScraperService scraperService,
    ICacheService cacheService,
    ILogger<TelegramBotService> logger) : ITelegramBotService
{
    public async Task UpdateMessageWorkflowAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var chatSettings = await cacheService.GetOrCreateAsync(update.Message!.Chat.Id.ToString(),
            async () => new ChatSettings());
        var botInfo = await botClient.GetMe(cancellationToken: cancellationToken);
        var message = new ParsedMessage(update.Message, botInfo.Username!, chatSettings);

        switch (message.Type)
        {
            case MessageEntityType.BotCommand:
                await ProcessBotCommandAsync(botClient, message, cancellationToken);
                return;
            case MessageEntityType.Url:
                await LinkHandlerAsync(botClient, message, cancellationToken);
                return;
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

        var botInfo = await botClient.GetMe(cancellationToken: cancellationToken);
        switch (message.BotCommand)
        {
            case var command when command == $"{CommandConstants.NO_MENTION_MODE_COMMAND}@{botInfo.Username}":
            {
                var keyboard = new InlineKeyboardMarkup([
                    [
                        InlineKeyboardButton.WithCallbackData(ButtonConstants.TURN_ON, true.ToString()),
                        InlineKeyboardButton.WithCallbackData(ButtonConstants.TURN_OFF, false.ToString())
                    ]
                ]);
                await botClient.SendMessage(chatId: message.ChatId, text: CommandConstants.NO_MENTION_MODE_TEXT,
                    replyMarkup: keyboard, cancellationToken: cancellationToken);
                await botClient.DeleteMessage(chatId: message.ChatId, messageId: message.Id,
                    cancellationToken: cancellationToken);
                break;
            }
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
            await botClient.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id,
                text: MessageConstants.ACCESS_DENIED,
                cancellationToken: cancellationToken);
            return;
        }

        var chatSettings = await cacheService.GetOrCreateAsync(model.ChatId.ToString(),
            async () => new ChatSettings());

        switch (model.Command)
        {
            case CommandConstants.NO_MENTION_MODE_TEXT:
            {
                if (bool.TryParse(model.Value, out var result))
                {
                    chatSettings.NeedMention = result;
                }

                await cacheService.SetAsync(model.ChatId.ToString(), chatSettings);
                await botClient.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id,
                    text: MessageConstants.SUCCESS, cancellationToken: cancellationToken);
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
        logger.LogInformation($"Received message to bot. ChatId: {message.ChatId}. Message: {message.Text}");

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
                await ProcessLinkMessageAsync(botClient, message, cancellationToken);
                break;
            default:
                return;
        }
    }
}