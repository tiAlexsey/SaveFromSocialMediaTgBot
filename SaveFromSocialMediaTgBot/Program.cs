using SaveFromSocialMediaTgBot.Extensions;
using SaveFromSocialMediaTgBot.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddConfigs();
builder.Services.AddServices(builder.Configuration);

builder.Services.AddHostedService<TelegramBotWorker>();

var host = builder.Build();
host.Run();