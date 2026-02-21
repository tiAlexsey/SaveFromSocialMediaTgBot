using SaveFromSocialMediaTgBot.Exceptions;
using SaveFromSocialMediaTgBot.Interfaces;
using SaveFromSocialMediaTgBot.Logging;
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
        using var _ = RequestContext.Push(update);
        logger.LogInformation("Handling update of type {UpdateType}", update.Type);

        try
        {
            switch (update)
            {
                case { Type: UpdateType.CallbackQuery }:
                    logger.LogInformation("Processing callback query from chat {ChatId}", update.CallbackQuery!.Message!.Chat.Id);
                    await telegramBotService.CallbackWorkflowAsync(botClient, update, cancellationToken);
                    return;

                case { Type: UpdateType.Message, Message.Type: MessageType.Text }:
                    logger.LogInformation("Processing text message from chat {ChatId}", update.Message!.Chat.Id);
                    await telegramBotService.UpdateWorkflowAsync(botClient, update, cancellationToken);
                    return;

                default:
                    logger.LogWarning("Received unsupported update type: {UpdateType}", update.Type);
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
            logger.LogError(ex, logMessage);
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