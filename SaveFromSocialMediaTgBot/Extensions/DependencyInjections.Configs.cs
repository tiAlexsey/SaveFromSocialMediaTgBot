namespace SaveFromSocialMediaTgBot.Extensions;

public static partial class DependencyInjections
{
    private static void AddConfigs(this IConfigurationManager manager)
    {
        manager.AddEnvironmentVariables();
    }
}