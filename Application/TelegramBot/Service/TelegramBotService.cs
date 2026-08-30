using Abstract.Data;
using Abstract.Data.Constants;
using Abstract.Interfaces;
using Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Data.Constants;
using TelegramBot.Data.Extensions;
using TelegramBot.Data.Models;

namespace TelegramBot.Service;

public interface ITelegramBotService
{
    Task UpdateWorkflowAsync(Update update, CancellationToken ct);
    Task CallbackWorkflowAsync(Update update, CancellationToken ct);
}

public class TelegramBotService(
    ScraperService scraperService,
    ICacheService cacheService,
    ITelegramBotClient client) : ITelegramBotService
{
    private const int MaxCaptionLength = 1024;
    private const int MaxMessageLength = 4000;

    public async Task UpdateWorkflowAsync(Update update, CancellationToken ct)
    {
        var chatSettings = await cacheService.GetOrCreateAsync(update.Message!.Chat.Id.ToString(),
            async () => new ChatSettings(), ct);
        var botInfo = await client.GetMe(cancellationToken: ct);
        var message = new ParsedMessage(update.Message, botInfo.Username!, chatSettings);

        switch (message.Type)
        {
            case MessageEntityType.BotCommand:
                await ProcessBotCommandAsync(message, ct);
                return;
            case MessageEntityType.Url:
                await LinkHandlerAsync(message, ct);
                return;
            default:
                return;
        }
    }

    public async Task CallbackWorkflowAsync(Update update, CancellationToken ct)
    {
        var data = update.CallbackQuery!.Data ?? string.Empty;
        var parts = data.Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var model = new
        {
            ChatId = update.CallbackQuery.Message!.Chat.Id,
            ChatType = update.CallbackQuery.Message!.Chat.Type,
            UserId = update.CallbackQuery.From.Id,
            Command = parts.Length > 0 ? parts[0] : string.Empty,
            Value = parts.Length > 1 ? parts[1] : string.Empty,
            MessageId = update.CallbackQuery.Message!.MessageId
        };

        if (!await IsAllowSettingsAsync(model.ChatId, model.UserId, model.ChatType, ct))
        {
            await client.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id,
                text: MessageConstants.AccessDenied,
                cancellationToken: ct);
            return;
        }

        var chatSettings = await cacheService.GetOrCreateAsync(model.ChatId.ToString(),
            async () => new ChatSettings(), ct);

        var changed = false;

        switch (model.Command)
        {
            case CommandConstants.CloseSettings:
            {
                await client.DeleteMessage(chatId: model.ChatId, messageId: model.MessageId,
                    cancellationToken: ct);
                break;
            }

            case CommandConstants.NotificationMode:
            {
                if (bool.TryParse(model.Value, out var result))
                {
                    chatSettings.Notification = result;
                    changed = true;
                }

                await cacheService.SetAsync(model.ChatId.ToString(), chatSettings, ct);
                await client.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id,
                    text: MessageConstants.Success, cancellationToken: ct);
                break;
            }

            case CommandConstants.MentionMode:
            {
                if (bool.TryParse(model.Value, out var result))
                {
                    chatSettings.Mention = result;
                    changed = true;
                }

                await cacheService.SetAsync(model.ChatId.ToString(), chatSettings, ct);
                await client.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id,
                    text: MessageConstants.Success, cancellationToken: ct);
                break;
            }

            case CommandConstants.DeleteOriginMessage:
            {
                if (bool.TryParse(model.Value, out var result))
                {
                    chatSettings.DeleteOriginMessage = result;
                    changed = true;
                }

                await cacheService.SetAsync(model.ChatId.ToString(), chatSettings, ct);
                await client.AnswerCallbackQuery(callbackQueryId: update.CallbackQuery.Id,
                    text: MessageConstants.Success, cancellationToken: ct);
                break;
            }
        }

        if (changed)
        {
            await client.EditMessageText(
                chatId: model.ChatId,
                messageId: model.MessageId,
                text: chatSettings.BuildSettingsText(),
                replyMarkup: chatSettings.BuildSettingsKeyboard(),
                cancellationToken: ct);
        }
    }

    private async Task ProcessBotCommandAsync(ParsedMessage message, CancellationToken ct)
    {
        if (!await IsAllowSettingsAsync(message.ChatId, message.UserId, message.ChatType, ct))
        {
            await client.DeleteMessage(message.ChatId, message.Id, ct);
            return;
        }

        var botInfo = await client.GetMe(cancellationToken: ct);

        switch (message.BotCommand)
        {
            case var command when command == $"{CommandConstants.Settings}@{botInfo.Username}" ||
                                  command == $"{CommandConstants.Settings}" & message.ChatType == ChatType.Private:
            {
                await client.SendMessage(
                    chatId: message.ChatId,
                    text: message.Settings.BuildSettingsText(),
                    replyMarkup: message.Settings.BuildSettingsKeyboard(),
                    cancellationToken: ct);

                await client.DeleteMessage(chatId: message.ChatId, messageId: message.Id,
                    cancellationToken: ct);

                break;
            }
        }
    }

    private async Task<bool> IsAllowSettingsAsync(long chatId, long userId, ChatType chatType, CancellationToken ct)
    {
        var member = await client.GetChatMember(chatId, userId, ct);
        return member.Status is ChatMemberStatus.Creator or ChatMemberStatus.Administrator
               || chatType == ChatType.Private;
    }

    private async Task LinkHandlerAsync(ParsedMessage message, CancellationToken ct)
    {
        switch (message.ChatType)
        {
            case ChatType.Group:
            case ChatType.Supergroup:
            case ChatType.Private:
            {
                if (message.Settings.Mention && !message.IsBotMention)
                    break;

                await ProcessLinkAsync(message, ct);
                break;
            }
            default:
                return;
        }
    }

    private async Task ProcessLinkAsync(ParsedMessage message, CancellationToken ct)
    {
        await client.SetMessageReaction(message.ChatId, message.Id, ["\ud83d\udc40"],
            cancellationToken: ct);

        var request = new ScrapedRequest(message.Link!, message.Parameters);
        var scraperResponse = await scraperService.GetSourceStreamAsync(request, ct);
        var text = scraperResponse.Results?.FirstOrDefault()?.Text;
        var caption = text?.Length <= MaxCaptionLength ? text : null;
        var textParts = caption is null
            ? SplitTextByParagraphs(text, MaxMessageLength)
            : [];
        var media = scraperResponse.ToInputMedia(caption);

        switch (media)
        {
            case { Count: > 10 }:
                foreach (var mediaChunk in media.Chunk(10))
                {
                    await client.SendMediaGroup(
                        chatId: message.ChatId,
                        media: mediaChunk,
                        messageThreadId: message.ThreadId,
                        disableNotification: message.Settings.Notification,
                        cancellationToken: ct);
                }

                break;
            case { Count: > 0 }:
                await client.SendMediaGroup(
                    chatId: message.ChatId,
                    media: media,
                    messageThreadId: message.ThreadId,
                    disableNotification: message.Settings.Notification,
                    cancellationToken: ct);
                break;
        }

        foreach (var textPart in textParts)
        {
            await client.SendMessage(
                chatId: message.ChatId,
                text: textPart,
                messageThreadId: message.ThreadId,
                disableNotification: message.Settings.Notification,
                cancellationToken: ct);
        }

        await client.SetMessageReaction(message.ChatId, message.Id, ["\ud83d\udcaf"],
            cancellationToken: ct);

        if (message.Settings.DeleteOriginMessage)
            await client.DeleteMessage(message.ChatId, message.Id, ct);
    }

    private static IReadOnlyList<string> SplitTextByParagraphs(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var paragraphs = text.Replace("\r\n", "\n").Split("\n\n");
        var chunks = new List<string>();
        var current = string.Empty;

        foreach (var paragraph in paragraphs)
        {
            var candidate = current.Length == 0 ? paragraph : $"{current}\n\n{paragraph}";
            if (candidate.Length <= maxLength)
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
            {
                chunks.Add(current);
                current = string.Empty;
            }

            for (var start = 0; start < paragraph.Length; start += maxLength)
            {
                var length = Math.Min(maxLength, paragraph.Length - start);
                if (start + length == paragraph.Length)
                    current = paragraph.Substring(start, length);
                else
                {
                    chunks.Add(paragraph.Substring(start, length));
                }
            }
        }

        if (current.Length > 0)
            chunks.Add(current);

        return chunks;
    }
}