using SaveFromSocialMediaTgBot.Abstract.Interface;
using SaveFromSocialMediaTgBot.Data.Const;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SaveFromSocialMediaTgBot;

public class TelegramBotWorker(
    ILogger<TelegramBotWorker> logger,
    IConfiguration configuration,
    ITelegramBotService telegramBotService) : BackgroundService
{
    private readonly TelegramBotClient client = new(configuration[Env.BOT_TOKEN] ?? throw new NullReferenceException());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        client.StartReceiving(
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
            switch (update)
            {
                case { Type: UpdateType.CallbackQuery }:
                    await telegramBotService.UpdateCallbackWorkflowAsync(botClient, update, cancellationToken);
                    return;
                case { Type: UpdateType.Message, Message.Type: MessageType.Text }:
                    await telegramBotService.UpdateMessageWorkflowAsync(botClient, update, cancellationToken);
                    return;
                default:
                    return;
            }
        }
        catch (Exception ex)
        {
            var logMessage = $"я обкакался, вот ошибка: {ex.Message}";
            logger.LogError(logMessage + "\n" + ex.Message + ex);
            await botClient.SetMessageReaction(update.Message!.Chat.Id, update.Message.Id, ["\ud83d\udca9"],
                cancellationToken: cancellationToken);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, exception.Message);
        return Task.CompletedTask;
    }
}