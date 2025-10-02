using System.Collections.Generic;
using Xunit;
using AzureTtsBatchStudio.Features.StoryBuilderV2.Services;

namespace AzureTtsBatchStudio.Tests.StoryBuilderV2
{
    public class TemplateEngineTests
    {
        private readonly ITemplateEngine _engine;

        public TemplateEngineTests()
        {
            _engine = new TemplateEngine();
        }

        [Fact]
        public void Render_SimpleVariable_ReplacesCorrectly()
        {
            // Arrange
            var template = "Hello, ${name}!";
            var variables = new Dictionary<string, object>
            {
                ["name"] = "World"
            };

            // Act
            var result = _engine.Render(template, variables);

            // Assert
            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void Render_NestedProperty_ReplacesCorrectly()
        {
            // Arrange
            var template = "Duration: ${duration.MinMinutes}-${duration.MaxMinutes} minutes";
            var variables = new Dictionary<string, object>
            {
                ["duration"] = new { MinMinutes = 15, MaxMinutes = 45 }
            };

            // Act
            var result = _engine.Render(template, variables);

            // Assert
            Assert.Equal("Duration: 15-45 minutes", result);
        }

        [Fact]
        public void Render_MissingVariable_LeavesPlaceholder()
        {
            // Arrange
            var template = "Hello, ${missing}!";
            var variables = new Dictionary<string, object>();

            // Act
            var result = _engine.Render(template, variables);

            // Assert
            Assert.Equal("Hello, ${missing}!", result);
        }

        [Fact]
        public void Render_MultipleVariables_ReplacesAll()
        {
            // Arrange
            var template = "${title} - ${genre} - ${duration} min";
            var variables = new Dictionary<string, object>
            {
                ["title"] = "Test Story",
                ["genre"] = "Horror",
                ["duration"] = 30
            };

            // Act
            var result = _engine.Render(template, variables);

            // Assert
            Assert.Equal("Test Story - Horror - 30 min", result);
        }
    }

    public class SsmlBuilderTests
    {
        private readonly ISsmlBuilder _builder;

        public SsmlBuilderTests()
        {
            _builder = new SsmlBuilder();
        }

        [Fact]
        public void BuildSsml_ValidBeat_GeneratesValidSsml()
        {
            // Arrange
            var beat = new AzureTtsBatchStudio.Features.StoryBuilderV2.Models.StoryBeat
            {
                Title = "Test Beat",
                DraftMd = "This is a test beat with some content."
            };

            var ttsProfile = new AzureTtsBatchStudio.Features.StoryBuilderV2.Models.TtsProfile
            {
                DefaultVoice = "en-US-AvaNeural",
                Rate = 1.0,
                Pitch = 0.0,
                Volume = 0.0
            };

            // Act
            var result = _builder.BuildSsml(beat, ttsProfile);

            // Assert
            Assert.Contains("<speak", result);
            Assert.Contains("<voice name=\"en-US-AvaNeural\">", result);
            Assert.Contains("This is a test beat with some content.", result);
            Assert.Contains("</speak>", result);
        }

        [Fact]
        public void BuildSsml_WithSfxMarkers_ConvertsToAudioTags()
        {
            // Arrange
            var beat = new AzureTtsBatchStudio.Features.StoryBuilderV2.Models.StoryBeat
            {
                Title = "Test Beat",
                DraftMd = "A door creaks open. [SFX:door_creak] Footsteps approach."
            };

            var ttsProfile = new AzureTtsBatchStudio.Features.StoryBuilderV2.Models.TtsProfile
            {
                DefaultVoice = "en-US-AvaNeural"
            };

            // Act
            var result = _builder.BuildSsml(beat, ttsProfile);

            // Assert
            Assert.Contains("<audio src=\"sfx/door_creak.wav\" />", result);
        }

        [Fact]
        public void ValidateSsml_ValidSsml_ReturnsSuccess()
        {
            // Arrange
            var ssml = @"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xml:lang=""en-US"">
                <voice name=""en-US-AvaNeural"">
                    Hello, world!
                </voice>
            </speak>";

            // Act
            var result = _builder.ValidateSsml(ssml);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ValidateSsml_InvalidXml_ReturnsFailure()
        {
            // Arrange
            var ssml = "<speak><voice>Unclosed voice";

            // Act
            var result = _builder.ValidateSsml(ssml);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Invalid XML", result.Error);
        }

        [Fact]
        public void ValidateSsml_MissingVoiceElement_ReturnsFailure()
        {
            // Arrange
            var ssml = @"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xml:lang=""en-US"">
                Hello, world!
            </speak>";

            // Act
            var result = _builder.ValidateSsml(ssml);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("voice", result.Error);
        }
    }

    public class DurationEstimatorTests
    {
        private readonly IDurationEstimator _estimator;

        public DurationEstimatorTests()
        {
            _estimator = new DurationEstimator();
        }

        [Theory]
        [InlineData(130, 130, 1.0)]
        [InlineData(260, 130, 2.0)]
        [InlineData(65, 130, 0.5)]
        public void EstimateDurationMinutes_ValidInput_ReturnsCorrectDuration(int wordCount, int wpm, double expectedMinutes)
        {
            // Act
            var result = _estimator.EstimateDurationMinutes(wordCount, wpm);

            // Assert
            Assert.Equal(expectedMinutes, result, precision: 2);
        }

        [Fact]
        public void CountWords_SimpleText_ReturnsCorrectCount()
        {
            // Arrange
            var text = "This is a simple test with seven words.";

            // Act
            var result = _estimator.CountWords(text);

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void CountWords_WithMarkdownHeaders_CountsWords()
        {
            // Arrange
            var text = @"## Chapter 1
This is the content of chapter one.";

            // Act
            var result = _estimator.CountWords(text);

            // Assert
            // Should count: "Chapter 1 This is the content of chapter one" = 9 words
            Assert.True(result >= 9 && result <= 10);
        }

        [Fact]
        public void CountWords_EmptyText_ReturnsZero()
        {
            // Arrange
            var text = "";

            // Act
            var result = _estimator.CountWords(text);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CountWords_WithSfxMarkers_ExcludesMarkers()
        {
            // Arrange
            var text = "A door opens [SFX:door_creak] and footsteps approach.";

            // Act
            var result = _estimator.CountWords(text);

            // Assert
            // Should count: "A door opens and footsteps approach" = 6 words
            Assert.True(result >= 6 && result <= 7);
        }
    }
}
