// Src/Infrastructure/Logging/CustomConsoleFormatter.cs

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Venue.Src.Infrastructure.Logging;

public sealed class CustomConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options) : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "custom";
    private readonly IOptionsMonitor<ConsoleFormatterOptions> _options = options;

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null && logEntry.Exception is null)
        {
            return;
        }

        var options = _options.CurrentValue;

        var timestamp = string.Empty;
        if (options.TimestampFormat is not null)
        {
            timestamp = (options.UseUtcTimestamp
                ? DateTimeOffset.UtcNow
                : DateTimeOffset.Now)
                .ToString(options.TimestampFormat, CultureInfo.InvariantCulture);
        }

        var level = GetLogLevelString(logEntry.LogLevel);

        var category = $"{logEntry.Category}[{logEntry.EventId.Id}]";

        var metadata = $"{timestamp} {level}: {ColorizeCategory(category)}";

        metadata = metadata.PadRight(110);

        textWriter.Write(metadata);
        textWriter.Write(" | ");

        if (!string.IsNullOrEmpty(message))
        {
            WriteMessage(textWriter, message);
        }

        if (logEntry.Exception is not null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                textWriter.Write(' ');
            }

            WriteMessage(textWriter, logEntry.Exception.ToString());
        }

        textWriter.WriteLine();
    }

    private static string ColorizeCategory(string category)
    {
        return $"{LoggerColors.CategoryBackgroundColor}" +
               $"{LoggerColors.CategoryForegroundColor}" +
               $"{category}" +
               $"{LoggerColors.Reset}";
    }

    private static void WriteMessage(TextWriter textWriter, string message)
    {
        textWriter.Write(message.Replace(Environment.NewLine, " "));
    }

    private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => throw new ArgumentOutOfRangeException(nameof(logLevel)),
    };
}