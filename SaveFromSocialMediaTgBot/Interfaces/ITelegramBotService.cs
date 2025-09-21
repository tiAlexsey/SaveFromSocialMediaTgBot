using Telegram.Bot;
using Telegram.Bot.Types;

namespace SaveFromSocialMediaTgBot.Interfaces;

public interface ITelegramBotService
{
    Task UpdateWorkflowAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
    Task CallbackWorkflowAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
}