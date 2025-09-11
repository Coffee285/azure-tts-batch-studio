using System;
using System.Linq;
using System.Xml.Linq;
using AzureTtsBatchStudio.Tts;
using Xunit;

namespace AzureTtsBatchStudio.Tests
{
    public class TtsChunkerTests
    {
        [Fact]
        public void SplitPlainText_ShouldRespectBudget()
        {
            // Arrange
            var options = new TtsRenderOptions
            {
                TargetChunkChars = 100,
                SafetyMarginChars = 20,
                MinChunkChars = 30
            };
            var chunker = new TtsChunker(options);
            var text = "This is a long sentence that should be split into multiple parts because it exceeds the budget. " +
                      "Here is another sentence that should go in a separate chunk.";
            
            // Act
            var parts = chunker.SplitPlainText(text, text => $"<speak>{text}</speak>");
            
            // Assert
            Assert.True(parts.Count > 1, "Text should be split into multiple parts");
            // Plain text should be within reasonable bounds (SSML wrapper adds chars)
            Assert.All(parts, part => Assert.True(part.PlainText.Length <= options.TargetChunkChars, 
                $"Plain text length {part.PlainText.Length} should be <= {options.TargetChunkChars}"));
        }

        [Fact]
        public void SplitPlainText_ShouldCoalesceSmallChunks()
        {
            // Arrange
            var options = new TtsRenderOptions
            {
                TargetChunkChars = 50,
                MinChunkChars = 40,
                SafetyMarginChars = 10
            };
            var chunker = new TtsChunker(options);
            var text = "Short. Very short. Tiny.";
            
            // Act
            var parts = chunker.SplitPlainText(text, text => $"<speak>{text}</speak>");
            
            // Assert
            Assert.Equal(1, parts.Count);
            Assert.Contains("Short", parts[0].PlainText);
            Assert.Contains("Very short", parts[0].PlainText);
            Assert.Contains("Tiny", parts[0].PlainText);
        }

        [Fact]
        public void SplitSsml_ShouldProduceWellFormedXml()
        {
            // Arrange
            var options = new TtsRenderOptions
            {
                TargetChunkChars = 100,
                SafetyMarginChars = 20
            };
            var chunker = new TtsChunker(options);
            var ssmlDoc = XDocument.Parse("<speak><voice name='test'>Hello world. This is a test.</voice></speak>");
            
            // Act
            var parts = chunker.SplitSsml(ssmlDoc, text => $"<speak><voice name='test'>{text}</voice></speak>");
            
            // Assert
            Assert.All(parts, part => 
            {
                // Should not throw when parsing SSML
                var doc = XDocument.Parse(part.SafeSsml);
                Assert.NotNull(doc);
            });
        }

        [Fact]
        public void SplitPlainText_EmptyText_ShouldReturnEmptyList()
        {
            // Arrange
            var options = new TtsRenderOptions();
            var chunker = new TtsChunker(options);
            
            // Act
            var parts = chunker.SplitPlainText("", text => $"<speak>{text}</speak>");
            
            // Assert
            Assert.Empty(parts);
        }

        [Fact]
        public void SplitPlainText_SingleSentenceUnderBudget_ShouldReturnSinglePart()
        {
            // Arrange
            var options = new TtsRenderOptions
            {
                TargetChunkChars = 100,
                SafetyMarginChars = 20
            };
            var chunker = new TtsChunker(options);
            var text = "This is a short sentence.";
            
            // Act
            var parts = chunker.SplitPlainText(text, text => $"<speak>{text}</speak>");
            
            // Assert
            Assert.Equal(1, parts.Count);
            Assert.Equal(text, parts[0].PlainText);
        }
    }
}