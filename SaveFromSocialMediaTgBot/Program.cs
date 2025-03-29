using SaveFromSocialMediaTgBot.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddConfigs();
builder.Services.AddServices();

var host = builder.Build();
host.Run();