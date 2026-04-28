// Infrastructure/Logging/LoggerFactoryProvider.cs

using Microsoft.Extensions.Logging;
namespace Sonyachna_Data_Forge.Infrastructure.Logging;
public static class LoggerFactoryProvider
{
    public static ILoggerFactory Create()
    {

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole( options =>
                {
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                    options.SingleLine = true;
                });
        });
        return loggerFactory;
    }
}