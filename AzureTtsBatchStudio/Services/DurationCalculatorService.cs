using System;

namespace AzureTtsBatchStudio.Services
{
    public interface IDurationCalculatorService
    {
        int CalculateTargetWords(int targetMinutes, int wordsPerMinute);
        double CalculateEstimatedMinutes(int currentWords, int wordsPerMinute);
        double CalculateProgress(int currentWords, int targetWords);
        string FormatProgressMessage(int currentWords, int targetWords, int wordsPerMinute);
    }

    public class DurationCalculatorService : IDurationCalculatorService
    {
        public int CalculateTargetWords(int targetMinutes, int wordsPerMinute)
        {
            if (targetMinutes <= 0 || wordsPerMinute <= 0)
                return 0;

            return targetMinutes * wordsPerMinute;
        }

        public double CalculateEstimatedMinutes(int currentWords, int wordsPerMinute)
        {
            if (currentWords <= 0 || wordsPerMinute <= 0)
                return 0;

            return (double)currentWords / wordsPerMinute;
        }

        public double CalculateProgress(int currentWords, int targetWords)
        {
            if (targetWords <= 0)
                return 0;

            return Math.Min(100.0, (double)currentWords / targetWords * 100.0);
        }

        public string FormatProgressMessage(int currentWords, int targetWords, int wordsPerMinute)
        {
            if (targetWords <= 0)
                return $"{currentWords} words";

            var progress = CalculateProgress(currentWords, targetWords);
            var estimatedMinutes = CalculateEstimatedMinutes(currentWords, wordsPerMinute);
            var targetMinutes = CalculateEstimatedMinutes(targetWords, wordsPerMinute);

            return $"{currentWords} / {targetWords} words ({progress:F1}%) • {estimatedMinutes:F1} / {targetMinutes:F1} minutes";
        }
    }
}