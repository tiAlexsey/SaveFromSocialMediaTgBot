using SaveFromSocialMediaTgBot.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.AddDependencyInjections();

var host = builder.Build();
host.Run();