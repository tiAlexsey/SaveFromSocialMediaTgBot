using SaveFromSocialMediaTgBot.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddConfigs();
builder.Services.AddServices(builder.Configuration);

var host = builder.Build();
host.Run();