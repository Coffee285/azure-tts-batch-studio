using System;
using Microsoft.Extensions.Logging;

namespace AzureTtsBatchStudio.Logging
{
    public sealed class LogEvent
    {
        public DateTime Timestamp { get; init; }
        public LogLevel Level { get; init; }
        public string Message { get; init; } = "";
        public Exception? Exception { get; init; }
        public string Category { get; init; } = "";

        public string LevelString => Level switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG", 
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            _ => "UNKNOWN"
        };

        public string FormattedMessage => Exception != null 
            ? $"{Message}\n{Exception}" 
            : Message;
    }
}