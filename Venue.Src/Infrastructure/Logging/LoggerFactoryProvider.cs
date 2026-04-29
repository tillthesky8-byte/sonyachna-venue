// Infrastructure/Logging/LoggerFactoryProvider.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
namespace Venue.Src.Infrastructure.Logging;
public static class LoggerFactoryProvider
{
    public static ILoggerFactory Create(LogLevel minimumLogLevel = LogLevel.Debug)
    {

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.Services.Configure<ConsoleFormatterOptions>(options =>
            {
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                options.UseUtcTimestamp = false;
                options.IncludeScopes = false;
            });
            builder.AddConsole(options =>
            {
                options.FormatterName = CustomConsoleFormatter.FormatterName;
            });

            builder.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
        });
        return loggerFactory;
    }
}