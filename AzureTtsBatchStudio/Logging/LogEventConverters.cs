using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Microsoft.Extensions.Logging;

namespace AzureTtsBatchStudio.Logging
{
    public static class LogEventConverters
    {
        public static readonly IValueConverter LevelToForegroundConverter = new LevelToForegroundValueConverter();
        public static readonly IValueConverter LevelToBackgroundConverter = new LevelToBackgroundValueConverter();
    }

    public class LevelToForegroundValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Error or LogLevel.Critical => Brushes.Red,
                    LogLevel.Warning => Brushes.Orange,
                    LogLevel.Information => Brushes.DodgerBlue,
                    LogLevel.Debug => Brushes.Gray,
                    LogLevel.Trace => Brushes.LightGray,
                    _ => Brushes.Black
                };
            }
            return Brushes.Black;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LevelToBackgroundValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Error or LogLevel.Critical => new SolidColorBrush(Color.FromArgb(20, 255, 0, 0)),
                    LogLevel.Warning => new SolidColorBrush(Color.FromArgb(20, 255, 165, 0)),
                    _ => Brushes.Transparent
                };
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}