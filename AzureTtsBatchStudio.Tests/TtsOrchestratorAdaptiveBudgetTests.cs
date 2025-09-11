using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureTtsBatchStudio.Models;
using AzureTtsBatchStudio.Services;
using AzureTtsBatchStudio.Tts;
using Xunit;

namespace AzureTtsBatchStudio.Tests
{
    public class TtsOrchestratorAdaptiveBudgetTests
    {
        [Fact]
        public void TtsChunker_LargeText_ShouldRespectBudget()
        {
            // Arrange
            var options = new TtsRenderOptions
            {
                TargetChunkChars = 100,
                SafetyMarginChars = 20,
                MinChunkChars = 30
            };
            var chunker = new TtsChunker(options);
            
            // Create text that should be split into multiple parts
            // Budget is 100-20=80 chars, so this should definitely be split
            var sentence1 = new string('A', 50); // 50 chars
            var sentence2 = new string('B', 50); // 50 chars  
            var text = $"{sentence1}. {sentence2}. More text here to exceed budget.";
            
            // Act
            var parts = chunker.SplitPlainText(text, text => $"<speak>{text}</speak>");
            
            // Assert
            Assert.True(parts.Count > 1, $"Large text should be split into multiple parts. Got {parts.Count} parts. Text length: {text.Length}");
            Assert.All(parts, part => Assert.True(part.PlainText.Length <= options.TargetChunkChars, 
                $"Plain text length {part.PlainText.Length} should be <= {options.TargetChunkChars}"));
        }

        [Fact]
        public void TtsChunker_LongSentence_ShouldSplitAtWhitespace()
        {
            // Arrange
            var options = new TtsRenderOptions
            {
                TargetChunkChars = 50,
                SafetyMarginChars = 10,
                MinChunkChars = 20
            };
            var chunker = new TtsChunker(options);
            
            // Create a single sentence longer than budget (this tests hard-wrap scenario)
            var words = Enumerable.Repeat("word", 20).ToArray();
            var longSentence = string.Join(" ", words) + "."; // ~100 chars
            
            // Act
            var parts = chunker.SplitPlainText(longSentence, text => $"<speak>{text}</speak>");
            
            // Assert
            Assert.True(parts.Count > 1, "Long sentence should be split into multiple parts");
            Assert.All(parts, part => 
            {
                Assert.True(part.PlainText.Length > 0, "Each part should have content");
                Assert.False(part.PlainText.Contains("  "), "Parts should not contain double spaces (indicating clean splits)");
            });
        }

        [Fact]
        public void TtsChunker_SmallChunks_ShouldCoalesce()
        {
            // Arrange
            var options = new TtsRenderOptions
            {
                TargetChunkChars = 100,
                MinChunkChars = 40,
                SafetyMarginChars = 10
            };
            var chunker = new TtsChunker(options);
            var text = "Short. Very short. Tiny. Small.";
            
            // Act
            var parts = chunker.SplitPlainText(text, text => $"<speak>{text}</speak>");
            
            // Assert
            Assert.Equal(1, parts.Count);
            Assert.Contains("Short", parts[0].PlainText);
            Assert.Contains("Very short", parts[0].PlainText);
            Assert.Contains("Tiny", parts[0].PlainText);
            Assert.Contains("Small", parts[0].PlainText);
        }

        [Fact]
        public void TtsRenderOptions_ShouldHaveCorrectDefaults()
        {
            // Arrange & Act
            var options = new TtsRenderOptions();
            
            // Assert
            Assert.Equal(MergeMode.SingleMergedMp3, options.MergeMode);
            Assert.Equal(2000, options.TargetChunkChars);
            Assert.Equal(1400, options.MinChunkChars);
            Assert.Equal(250, options.SafetyMarginChars); // Updated from 200 to 250 per problem statement
            Assert.True(options.RespectSentenceBoundaries);
            Assert.True(options.KeepShortParagraphsTogether);
        }
    }
}