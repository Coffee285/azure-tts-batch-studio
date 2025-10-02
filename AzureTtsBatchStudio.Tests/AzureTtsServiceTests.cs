using System;
using System.Threading.Tasks;
using AzureTtsBatchStudio.Models;
using AzureTtsBatchStudio.Services;
using Microsoft.CognitiveServices.Speech;
using Xunit;

namespace AzureTtsBatchStudio.Tests
{
    public class AzureTtsServiceTests
    {
        [Fact]
        public void DetectSSML_ShouldRecognizeValidSSML()
        {
            // Arrange
            var validSsml = @"<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis'>
    <voice name='en-US-AriaNeural'>
        <prosody rate='medium' pitch='default'>
            Hello world
        </prosody>
    </voice>
</speak>";
            
            // Act - Using reflection to test the private method
            var service = new AzureTtsService();
            var method = typeof(AzureTtsService).GetMethod("DetectSSML", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { validSsml });
            
            // Assert
            Assert.True(result, "Valid SSML should be detected");
        }

        [Fact]
        public void DetectSSML_ShouldRejectPlainText()
        {
            // Arrange
            var plainText = "This is just plain text without any SSML markup.";
            
            // Act - Using reflection to test the private method
            var service = new AzureTtsService();
            var method = typeof(AzureTtsService).GetMethod("DetectSSML", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { plainText });
            
            // Assert
            Assert.False(result, "Plain text should not be detected as SSML");
        }

        [Fact]
        public void DetectSSML_ShouldRejectIncompleteSSML()
        {
            // Arrange
            var incompleteSsml = "<speak>Hello world"; // Missing closing tag
            
            // Act - Using reflection to test the private method
            var service = new AzureTtsService();
            var method = typeof(AzureTtsService).GetMethod("DetectSSML", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { incompleteSsml });
            
            // Assert
            Assert.False(result, "Incomplete SSML should not be detected as valid SSML");
        }

        [Theory]
        [InlineData("onyx", false, "Onyx voice should not support prosody")]
        [InlineData("nova", false, "Nova voice should not support prosody")]
        [InlineData("shimmer", false, "Shimmer voice should not support prosody")]
        [InlineData("echo", false, "Echo voice should not support prosody")]
        [InlineData("fable", false, "Fable voice should not support prosody")]
        [InlineData("alloy", false, "Alloy voice should not support prosody")]
        [InlineData("gpt-4-turbo", false, "GPT-4 Turbo should not support prosody")]
        [InlineData("en-US-AriaNeural", true, "Aria Neural should support prosody")]
        [InlineData("en-US-GuyNeural", true, "Guy Neural should support prosody")]
        [InlineData("en-GB-SoniaNeural", true, "Sonia Neural should support prosody")]
        public void IsVoiceWithoutProsodySupport_ShouldDetectCorrectly(string voiceName, bool expectedSupport, string reason)
        {
            // Act - Using reflection to test the private method
            var service = new AzureTtsService();
            var method = typeof(AzureTtsService).GetMethod("IsVoiceWithoutProsodySupport", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { voiceName });
            
            // Assert
            var actualSupport = !result; // Method returns true if NO support, so invert for the test
            Assert.Equal(expectedSupport, actualSupport);
        }

        [Fact]
        public void GenerateSsml_ShouldExcludeProsodyForUnsupportedVoices()
        {
            // Arrange - Using reflection to test the private method
            var service = new AzureTtsService();
            var method = typeof(AzureTtsService).GetMethod("GenerateSsml", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            var text = "Hello world";
            var voiceName = "onyx"; // Unsupported voice
            var rate = 1.5;
            var pitch = 10.0;
            
            // Act
            var result = (string)method.Invoke(null, new object[] { text, voiceName, rate, pitch });
            
            // Assert
            Assert.DoesNotContain("<prosody", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Hello world", result);
            Assert.Contains($"<voice name='{voiceName}'>", result);
        }

        [Fact]
        public void GenerateSsml_ShouldIncludeProsodyForSupportedVoices()
        {
            // Arrange - Using reflection to test the private method
            var service = new AzureTtsService();
            var method = typeof(AzureTtsService).GetMethod("GenerateSsml", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            var text = "Hello world";
            var voiceName = "en-US-AriaNeural"; // Supported voice
            var rate = 1.5;
            var pitch = 10.0;
            
            // Act
            var result = (string)method.Invoke(null, new object[] { text, voiceName, rate, pitch });
            
            // Assert
            Assert.Contains("<prosody", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Hello world", result);
            Assert.Contains($"<voice name='{voiceName}'>", result);
        }
    }
}