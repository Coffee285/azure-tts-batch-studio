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
    }
}