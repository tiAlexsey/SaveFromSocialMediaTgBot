using SaveFromSocialMediaTgBot.Exceptions;
using SaveFromSocialMediaTgBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SaveFromSocialMediaTgBot;

public class TelegramBotWorker(
    ILogger<TelegramBotWorker> logger,
    ITelegramBotClient client,
    ITelegramBotService telegramBotService) : BackgroundService
{
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
                    await telegramBotService.CallbackWorkflowAsync(botClient, update, cancellationToken);
                    return;
                case { Type: UpdateType.Message, Message.Type: MessageType.Text }:
                    await telegramBotService.UpdateWorkflowAsync(botClient, update, cancellationToken);
                    return;
                default:
                    return;
            }
        }
        catch (InvalidUrlException ex)
        {
            await botClient.SetMessageReaction(update.Message!.Chat.Id, update.Message.Id, [],
                cancellationToken: cancellationToken);
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