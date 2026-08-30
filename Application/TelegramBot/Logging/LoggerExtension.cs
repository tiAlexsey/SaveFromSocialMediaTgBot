using Serilog;

namespace TelegramBot.Logging;

public static class LoggerExtension
{
    internal static IServiceCollection ConfigureLogging(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Filter.ByIncludingOnly(evt =>
            {
                if (evt.Properties.TryGetValue("SourceContext", out var ctx))
                {
                    var source = ctx.ToString().Trim('"');
                    if (source.StartsWith("System.Net.Http.HttpClient.IVideoScraper.LogicalHandler") ||
                        source.StartsWith("System.Net.Http.HttpClient.IVideoScraper.ClientHandler"))
                    {
                        return evt.Level >= Serilog.Events.LogEventLevel.Warning;
                    }
                }

                return true;
            })
            .Enrich.With<RequestContextEnricher>()
            .CreateLogger();

        services.AddSerilog();

        return services;
    }
}