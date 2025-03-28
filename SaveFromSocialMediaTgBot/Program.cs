using SaveFromSocialMediaTgBot.Extensions;
using SaveFromSocialMediaTgBot.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddServices();

builder.Services.AddHostedService<TelegramBotWorker>();

var host = builder.Build();
host.Run();