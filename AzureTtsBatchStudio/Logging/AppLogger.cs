using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AzureTtsBatchStudio.Logging
{
    public sealed class AppLogger : IDisposable
    {
        private readonly Logger _serilogLogger;
        private readonly LogViewModel _logViewModel;
        private readonly ILoggerFactory _loggerFactory;

        public LogViewModel LogViewModel => _logViewModel;

        public AppLogger()
        {
            _logViewModel = new LogViewModel();

            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                     "AzureTtsBatchStudio", "logs");
            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, "app-.log");

            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFile, 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Sink(new InMemoryLogSink(_logViewModel))
                .CreateLogger();

            _loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSerilog(_serilogLogger));
        }

        public ILogger<T> GetLogger<T>() => _loggerFactory.CreateLogger<T>();
        public Microsoft.Extensions.Logging.ILogger GetLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);

        public void Dispose()
        {
            _serilogLogger?.Dispose();
            _loggerFactory?.Dispose();
        }
    }

    internal sealed class InMemoryLogSink : ILogEventSink
    {
        private readonly LogViewModel _logViewModel;

        public InMemoryLogSink(LogViewModel logViewModel)
        {
            _logViewModel = logViewModel;
        }

        public void Emit(Serilog.Events.LogEvent logEvent)
        {
            var level = ConvertLevel(logEvent.Level);
            var message = logEvent.RenderMessage();
            var exception = logEvent.Exception;

            var logEventItem = new LogEvent
            {
                Timestamp = logEvent.Timestamp.DateTime,
                Level = level,
                Message = message,
                Exception = exception,
                Category = ExtractCategory(logEvent)
            };

            _logViewModel.AddLogEvent(logEventItem);
        }

        private static LogLevel ConvertLevel(Serilog.Events.LogEventLevel serilogLevel)
        {
            return serilogLevel switch
            {
                Serilog.Events.LogEventLevel.Verbose => LogLevel.Trace,
                Serilog.Events.LogEventLevel.Debug => LogLevel.Debug,
                Serilog.Events.LogEventLevel.Information => LogLevel.Information,
                Serilog.Events.LogEventLevel.Warning => LogLevel.Warning,
                Serilog.Events.LogEventLevel.Error => LogLevel.Error,
                Serilog.Events.LogEventLevel.Fatal => LogLevel.Critical,
                _ => LogLevel.Information
            };
        }

        private static string ExtractCategory(Serilog.Events.LogEvent logEvent)
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext) &&
                sourceContext is Serilog.Events.ScalarValue { Value: string category })
            {
                return category;
            }
            return "App";
        }
    }
}