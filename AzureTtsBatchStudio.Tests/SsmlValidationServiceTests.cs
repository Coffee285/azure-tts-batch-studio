using Xunit;
using AzureTtsBatchStudio.Services;
using AzureTtsBatchStudio.Models;
using System.Collections.Generic;

namespace AzureTtsBatchStudio.Tests
{
    public class SsmlValidationServiceTests
    {
        private readonly ISsmlValidationService _validationService;

        public SsmlValidationServiceTests()
        {
            _validationService = new SsmlValidationService();
        }

        [Fact]
        public void ValidateSsml_WithValidSsml_ReturnsValid()
        {
            // Arrange
            var ssml = @"<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis'>
                <voice name='en-US-AriaNeural'>
                    <prosody rate='medium' pitch='default'>
                        Hello world!
                    </prosody>
                </voice>
            </speak>";
            
            var voice = new VoiceInfo
            {
                Name = "en-US-AriaNeural",
                DisplayName = "Aria",
                SupportsSpeakingRate = true,
                SupportsPitch = true
            };

            // Act
            var result = _validationService.ValidateSsml(ssml, voice);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateSsml_WithMalformedXml_ReturnsInvalid()
        {
            // Arrange
            var ssml = @"<speak>
                <voice name='en-US-AriaNeural'>
                    <prosody rate='medium'>
                        Hello world!
                    <!-- Missing closing tags -->
            </speak>";
            
            var voice = new VoiceInfo
            {
                Name = "en-US-AriaNeural",
                SupportsSpeakingRate = true
            };

            // Act
            var result = _validationService.ValidateSsml(ssml, voice);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("not well-formed", result.Errors[0]);
        }

        [Fact]
        public void ValidateSsml_WithEmptyString_ReturnsInvalid()
        {
            // Arrange
            var ssml = "";
            var voice = new VoiceInfo { Name = "en-US-AriaNeural" };

            // Act
            var result = _validationService.ValidateSsml(ssml, voice);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("empty", result.Errors[0].ToLowerInvariant());
        }

        [Fact]
        public void ValidateSsml_WithProsodyUnsupportedVoice_ReturnsWarning()
        {
            // Arrange
            var ssml = @"<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis'>
                <voice name='alloy'>
                    <prosody rate='fast' pitch='+10%'>
                        Hello world!
                    </prosody>
                </voice>
            </speak>";
            
            var voice = new VoiceInfo
            {
                Name = "alloy",
                DisplayName = "Alloy (OpenAI)",
                SupportsSpeakingRate = false,
                SupportsPitch = false
            };

            // Act
            var result = _validationService.ValidateSsml(ssml, voice);

            // Assert
            Assert.True(result.IsValid); // Structure is valid
            Assert.NotEmpty(result.Warnings); // But has warnings
            Assert.Contains("does not support", result.Warnings[0]);
        }

        [Fact]
        public void ValidateSsml_WithStyleUnsupportedVoice_ReturnsWarning()
        {
            // Arrange
            var ssml = @"<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis' 
                xmlns:mstts='http://www.w3.org/2001/mstts'>
                <voice name='en-US-GuyNeural'>
                    <mstts:express-as style='cheerful'>
                        Hello world!
                    </mstts:express-as>
                </voice>
            </speak>";
            
            var voice = new VoiceInfo
            {
                Name = "en-US-GuyNeural",
                DisplayName = "Guy",
                SupportsStyle = false
            };

            // Act
            var result = _validationService.ValidateSsml(ssml, voice);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotEmpty(result.Warnings);
        }

        [Fact]
        public void ValidateBatchRequest_WithValidInputs_ReturnsValid()
        {
            // Arrange
            var inputText = "Hello, this is a test.";
            var voice = new VoiceInfo
            {
                Name = "en-US-AriaNeural",
                DisplayName = "Aria"
            };
            var outputDirectory = System.IO.Path.GetTempPath();

            // Act
            var result = _validationService.ValidateBatchRequest(inputText, voice, outputDirectory);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateBatchRequest_WithEmptyText_ReturnsInvalid()
        {
            // Arrange
            var inputText = "";
            var voice = new VoiceInfo { Name = "en-US-AriaNeural" };
            var outputDirectory = System.IO.Path.GetTempPath();

            // Act
            var result = _validationService.ValidateBatchRequest(inputText, voice, outputDirectory);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("required", result.Errors[0].ToLowerInvariant());
        }

        [Fact]
        public void ValidateBatchRequest_WithNullVoice_ReturnsInvalid()
        {
            // Arrange
            var inputText = "Hello, this is a test.";
            VoiceInfo? voice = null;
            var outputDirectory = System.IO.Path.GetTempPath();

            // Act
            var result = _validationService.ValidateBatchRequest(inputText, voice!, outputDirectory);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("voice", result.Errors[0].ToLowerInvariant());
        }

        [Fact]
        public void ValidateBatchRequest_WithInvalidDirectory_ReturnsInvalid()
        {
            // Arrange
            var inputText = "Hello, this is a test.";
            var voice = new VoiceInfo { Name = "en-US-AriaNeural" };
            var outputDirectory = "/nonexistent/path/that/does/not/exist";

            // Act
            var result = _validationService.ValidateBatchRequest(inputText, voice, outputDirectory);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("does not exist", result.Errors[0]);
        }

        [Fact]
        public void ValidationResult_GetSummary_ReturnsFormattedString()
        {
            // Arrange
            var result = new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Error 1", "Error 2" },
                Warnings = new List<string> { "Warning 1" }
            };

            // Act
            var summary = result.GetSummary();

            // Assert
            Assert.Contains("❌", summary);
            Assert.Contains("⚠️", summary);
            Assert.Contains("Error 1", summary);
            Assert.Contains("Warning 1", summary);
        }
    }
}
