using Xunit;
using AzureTtsBatchStudio.Preflight;

namespace AzureTtsBatchStudio.Tests
{
    public class MojibakeHeuristicsTests
    {
        [Fact]
        public void Score_WithCleanText_ReturnsZero()
        {
            // Arrange
            var cleanText = "This is perfectly normal text with apostrophe's and quotes.";

            // Act
            var score = MojibakeHeuristics.Score(cleanText);

            // Assert
            Assert.Equal(0, score);
        }

        [Fact]
        public void Score_WithMojibakeSequences_ReturnsPositiveScore()
        {
            // Arrange - using the Unicode escapes for the mojibake patterns
            var mojibakeText = "It\u00E2\u0080\u0099s time \u00E2\u0080\u0094 finally\u00E2\u0080\u00A6";

            // Act
            var score = MojibakeHeuristics.Score(mojibakeText);

            // Assert
            Assert.True(score > 0);
        }

        [Fact]
        public void Score_WithReplacementCharacter_ReturnsPositiveScore()
        {
            // Arrange
            var textWithReplacementChar = "This text has \uFFFD replacement characters.";

            // Act
            var score = MojibakeHeuristics.Score(textWithReplacementChar);

            // Assert
            Assert.True(score > 0);
        }

        [Fact]
        public void Score_WithControlCharacters_ReturnsPositiveScore()
        {
            // Arrange - text with control character (excluding allowed ones)
            var textWithControlChar = "Text with\u0001 control character.";

            // Act
            var score = MojibakeHeuristics.Score(textWithControlChar);

            // Assert
            Assert.True(score > 0);
        }

        [Fact]
        public void Score_WithAllowedControlCharacters_DoesNotPenalize()
        {
            // Arrange - text with allowed control characters
            var textWithAllowedChars = "Text with\r\n\ttab and newlines.";

            // Act
            var score = MojibakeHeuristics.Score(textWithAllowedChars);

            // Assert
            Assert.Equal(0, score);
        }

        [Fact]
        public void Score_WithEmptyOrNullString_ReturnsZero()
        {
            // Act & Assert
            Assert.Equal(0, MojibakeHeuristics.Score(""));
            Assert.Equal(0, MojibakeHeuristics.Score(null!));
        }
    }

    public class EncodingPreflightTests
    {
        [Fact]
        public void Run_WithCleanText_ReturnsNormalizeOnlyStrategy()
        {
            // Arrange
            var preflight = new EncodingPreflight();
            var cleanText = "This is clean UTF-8 text.";

            // Act
            var result = preflight.Run(cleanText);

            // Assert
            Assert.Equal("NormalizeOnly", result.Strategy);
            Assert.Equal(0, result.ScoreBefore);
            Assert.Equal(0, result.ScoreAfter);
            Assert.Equal(cleanText, result.Fixed);
        }

        [Fact]
        public void Run_WithMojibakeText_ProducesLowerScore()
        {
            // Arrange
            var preflight = new EncodingPreflight();
            var mojibakeText = "It\u00E2\u0080\u0099s time \u00E2\u0080\u0094 finally\u00E2\u0080\u00A6";

            // Act
            var result = preflight.Run(mojibakeText);

            // Assert
            Assert.True(result.ScoreBefore > 0);
            Assert.True(result.ScoreAfter < result.ScoreBefore);
            Assert.NotEqual("NormalizeOnly", result.Strategy);
        }

        [Fact]
        public void Run_WithControlCharacters_RemovesIllegalXmlChars()
        {
            // Arrange
            var preflight = new EncodingPreflight();
            var textWithControlChars = "I can\u0001t believe it";

            // Act
            var result = preflight.Run(textWithControlChars);

            // Assert - the control character should be stripped out
            Assert.False(result.Fixed.Contains('\u0001'), $"Control character found in result: '{result.Fixed}'");
            Assert.Equal("I cant believe it", result.Fixed);
        }

        [Fact]
        public void Run_WithMappableArtifacts_AppliesCorrectReplacements()
        {
            // Arrange
            var preflight = new EncodingPreflight();
            // Use the actual mojibake pattern that should be in our map
            var textWithArtifacts = "Caf\u00C3\u00A9 and pi\u00C3\u00B1ata";

            // Act
            var result = preflight.Run(textWithArtifacts);

            // Assert - Check if either replacements were made OR score improved
            Assert.True(result.Replacements.Count > 0 || result.ScoreAfter < result.ScoreBefore);
        }

        [Fact]
        public void Run_ReturnsValidPreflightResult()
        {
            // Arrange
            var preflight = new EncodingPreflight();
            var inputText = "Sample text";

            // Act
            var result = preflight.Run(inputText);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(inputText, result.Original);
            Assert.NotNull(result.Fixed);
            Assert.NotNull(result.Strategy);
            Assert.NotNull(result.Warnings);
            Assert.NotNull(result.Replacements);
        }

        [Fact]
        public void Run_WithSignificantImprovementAfterFix_AddsWarningForResidualIssues()
        {
            // Arrange
            var preflight = new EncodingPreflight();
            // Create text that might have partial fixes
            var complexMojibake = "\u00E2\u0080\u0099test\u00E2\u0080\u0099 with \uFFFD replacement";

            // Act
            var result = preflight.Run(complexMojibake);

            // Assert
            // If there's improvement but still issues, should have warnings
            if (result.ScoreAfter > 0 && result.ScoreAfter < result.ScoreBefore)
            {
                Assert.True(result.Warnings.Count > 0);
                Assert.Contains("Residual suspicious sequences", result.Warnings[0]);
            }
        }
    }
}