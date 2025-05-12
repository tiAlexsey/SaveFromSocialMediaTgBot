namespace SaveFromSocialMediaTgBot.Extensions;

public static partial class DependencyInjections
{
    public static void AddDependencyInjections(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddConfigs();
        builder.Services.AddServices(builder.Configuration);
    }
}