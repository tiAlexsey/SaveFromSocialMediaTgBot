using Abstract.Data.Constants;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Data.Constants;
using TelegramBot.Data.Models;

namespace TelegramBot.Data.Extensions;

public static class ChatSettingsExtensions
{
    extension(ChatSettings s)
    {
        public string BuildSettingsText() =>
            $"⚙️ {MessageConstants.Settings}\n" +
            $"• {MessageConstants.MentionMode}: {(s.Mention
                ? MessageConstants.MentionModeOn
                : MessageConstants.MentionModeOff)}\n" +
            $"• {MessageConstants.NotificationMode}: {(s.Notification
                ? MessageConstants.NotificationModeOn
                : MessageConstants.NotificationModeOff)}\n" +
            $"• {MessageConstants.DeleteOriginMessage}: {(s.DeleteOriginMessage
                ? MessageConstants.DeleteOriginMessageOn
                : MessageConstants.DeleteOriginMessageOff)}\n";

        public InlineKeyboardMarkup BuildSettingsKeyboard() => new([
            [
                InlineKeyboardButton.WithCallbackData(
                    s.Mention
                        ? ButtonConstants.MentionModeOff
                        : ButtonConstants.MentionModeOn,
                    $"{CommandConstants.MentionMode}:{(!s.Mention).ToString().ToLower()}")
            ],
            [
                InlineKeyboardButton.WithCallbackData(
                    s.Notification
                        ? ButtonConstants.NotificationModeOff
                        : ButtonConstants.NotificationModeOn,
                    $"{CommandConstants.NotificationMode}:{(!s.Notification).ToString().ToLower()}")
            ],
            [
                InlineKeyboardButton.WithCallbackData(
                    s.DeleteOriginMessage
                        ? ButtonConstants.DeleteOriginMessageOff
                        : ButtonConstants.DeleteOriginMessageOn,
                    $"{CommandConstants.DeleteOriginMessage}:{(!s.DeleteOriginMessage).ToString().ToLower()}")
            ],
            [
                InlineKeyboardButton.WithCallbackData(ButtonConstants.CloseSettings,
                    CommandConstants.CloseSettings)
            ]
        ]);
    }
}