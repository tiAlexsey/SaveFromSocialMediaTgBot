namespace SaveFromSocialMediaTgBot.Extensions;

public static partial class DependencyInjections
{
    public static void AddConfigs(this IConfigurationManager manager)
    {
        manager.AddEnvironmentVariables();
    }
}