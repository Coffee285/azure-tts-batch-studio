using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AzureTtsBatchStudio.ViewModels
{
    public class EnumToBoolConverter : IValueConverter
    {
        public static readonly EnumToBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Enum enumValue && parameter is string paramString)
            {
                return enumValue.ToString().Equals(paramString, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter is string paramString)
            {
                if (Enum.TryParse(targetType, paramString, true, out var result))
                {
                    return result;
                }
            }
            return Avalonia.Data.BindingOperations.DoNothing;
        }
    }
}