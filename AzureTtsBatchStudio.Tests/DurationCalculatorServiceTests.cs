using Xunit;
using AzureTtsBatchStudio.Services;

namespace AzureTtsBatchStudio.Tests
{
    public class DurationCalculatorServiceTests
    {
        [Fact]
        public void CalculateTargetWords_WithValidInputs_ReturnsCorrectResult()
        {
            // Arrange
            var calculator = new DurationCalculatorService();

            // Act
            var result = calculator.CalculateTargetWords(10, 170);

            // Assert
            Assert.Equal(1700, result);
        }

        [Fact]
        public void CalculateEstimatedMinutes_WithValidInputs_ReturnsCorrectResult()
        {
            // Arrange
            var calculator = new DurationCalculatorService();

            // Act
            var result = calculator.CalculateEstimatedMinutes(850, 170);

            // Assert
            Assert.Equal(5.0, result);
        }

        [Fact]
        public void CalculateProgress_WithPartialCompletion_ReturnsCorrectPercentage()
        {
            // Arrange
            var calculator = new DurationCalculatorService();

            // Act
            var result = calculator.CalculateProgress(500, 1000);

            // Assert
            Assert.Equal(50.0, result);
        }

        [Fact]
        public void CalculateProgress_WithOverCompletion_CapsAt100Percent()
        {
            // Arrange
            var calculator = new DurationCalculatorService();

            // Act
            var result = calculator.CalculateProgress(1200, 1000);

            // Assert
            Assert.Equal(100.0, result);
        }

        [Fact]
        public void FormatProgressMessage_WithValidInputs_ReturnsFormattedString()
        {
            // Arrange
            var calculator = new DurationCalculatorService();

            // Act
            var result = calculator.FormatProgressMessage(500, 1000, 170);

            // Assert
            Assert.Contains("500 / 1000 words", result);
            Assert.Contains("50.0%", result);
            Assert.Contains("2.9", result); // 500/170 ≈ 2.9 minutes
            Assert.Contains("5.9", result); // 1000/170 ≈ 5.9 minutes
        }
    }
}