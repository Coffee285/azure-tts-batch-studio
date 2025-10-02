using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureTtsBatchStudio.Infrastructure.Common
{
    /// <summary>
    /// Guard clauses for argument validation
    /// </summary>
    public static class Guard
    {
        public static void AgainstNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
        }

        public static void AgainstNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }

        public static void AgainstNullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
        }

        public static void AgainstNegative(int value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
        }

        public static void AgainstNegative(double value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
        }

        public static void AgainstOutOfRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(paramName, 
                    $"{paramName} must be between {min} and {max}.");
        }

        public static void AgainstOutOfRange(double value, double min, double max, string paramName)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(paramName, 
                    $"{paramName} must be between {min} and {max}.");
        }

        public static void AgainstEmptyCollection<T>(IEnumerable<T> value, string paramName)
        {
            if (value == null || !value.Any())
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }
    }
}
