using Abstract.Data.Constants;
using Services;
using Telegram.Bot;
using TelegramBot;
using TelegramBot.Logging;
using TelegramBot.Service;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.ConfigureLogging(configuration)
    .AddCache(configuration, instance: "Chat-settings:")
    .AddVideoScrapers();

services.AddTransient<ITelegramBotService, TelegramBotService>();
services.AddSingleton<ITelegramBotClient, TelegramBotClient>(_ =>
{
    var options = new TelegramBotClientOptions(configuration[EnvironmentConstants.BotToken]!);
    return new TelegramBotClient(options);
});

services.AddHostedService<TelegramBotWorker>();

var host = builder.Build();
host.Run();