using Telegram.Bot;
using Telegram.Bot.Types;

namespace SaveFromSocialMediaTgBot.Interfaces;

public interface ITelegramBotService
{
    Task UpdateMessageWorkflowAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
    Task UpdateCallbackWorkflowAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
}