namespace SaveFromSocialMediaTgBot.Extensions;

public static partial class AppBuilderExtension
{
    private static void AddConfigs(this IConfigurationManager manager)
    {
        manager.AddEnvironmentVariables();
    }
}