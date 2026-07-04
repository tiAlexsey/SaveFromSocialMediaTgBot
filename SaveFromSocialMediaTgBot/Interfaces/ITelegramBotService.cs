using Telegram.Bot.Types;

namespace SaveFromSocialMediaTgBot.Interfaces;

public interface ITelegramBotService
{
    Task UpdateWorkflowAsync(Update update, CancellationToken ct);
    Task CallbackWorkflowAsync(Update update, CancellationToken ct);
}