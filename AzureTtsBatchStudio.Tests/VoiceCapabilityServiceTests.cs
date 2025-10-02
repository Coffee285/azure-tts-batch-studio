using Xunit;
using AzureTtsBatchStudio.Services;
using AzureTtsBatchStudio.Models;
using System.Threading.Tasks;

namespace AzureTtsBatchStudio.Tests
{
    public class VoiceCapabilityServiceTests
    {
        private readonly IVoiceCapabilityService _capabilityService;

        public VoiceCapabilityServiceTests()
        {
            _capabilityService = new VoiceCapabilityService();
        }

        [Theory]
        [InlineData("en-US-AriaNeural", true, true, "Aria supports styles")]
        [InlineData("en-US-GuyNeural", true, true, "Guy supports styles")]
        [InlineData("zh-CN-XiaoxiaoNeural", true, true, "Xiaoxiao supports styles")]
        [InlineData("ja-JP-NanamiNeural", true, true, "Nanami supports styles")]
        public async Task GetVoiceCapabilities_StyleSupportedVoices_ReturnsStyleSupport(
            string voiceName, bool expectedStyle, bool expectedDegree, string reason)
        {
            // Act
            var capabilities = await _capabilityService.GetVoiceCapabilitiesAsync(voiceName);

            // Assert
            Assert.NotNull(capabilities);
            Assert.Equal(expectedStyle, capabilities.SupportsStyle);
            Assert.Equal(expectedDegree, capabilities.SupportsStyleDegree);
            if (expectedStyle)
            {
                Assert.NotEmpty(capabilities.AvailableStyles);
            }
        }

        [Theory]
        [InlineData("en-US-AriaNeural", true, "Aria supports roles")]
        [InlineData("en-US-JennyNeural", true, "Jenny supports roles")]
        [InlineData("zh-CN-XiaoxiaoNeural", true, "Xiaoxiao supports roles")]
        public async Task GetVoiceCapabilities_RoleSupportedVoices_ReturnsRoleSupport(
            string voiceName, bool expectedRole, string reason)
        {
            // Act
            var capabilities = await _capabilityService.GetVoiceCapabilitiesAsync(voiceName);

            // Assert
            Assert.NotNull(capabilities);
            Assert.Equal(expectedRole, capabilities.SupportsRole);
            if (expectedRole)
            {
                Assert.NotEmpty(capabilities.AvailableRoles);
            }
        }

        [Theory]
        [InlineData("alloy", false, "OpenAI TTS voice")]
        [InlineData("nova", false, "OpenAI TTS voice")]
        [InlineData("en-GB-LibbyNeural", false, "Libby doesn't support styles")]
        public async Task GetVoiceCapabilities_UnsupportedVoices_ReturnsNoStyle(
            string voiceName, bool expectedStyle, string reason)
        {
            // Act
            var capabilities = await _capabilityService.GetVoiceCapabilitiesAsync(voiceName);

            // Assert
            Assert.NotNull(capabilities);
            Assert.Equal(expectedStyle, capabilities.SupportsStyle);
        }

        [Fact]
        public async Task EnrichVoiceInfo_WithSupportedVoice_AddsCapabilities()
        {
            // Arrange
            var voice = new VoiceInfo
            {
                Name = "en-US-AriaNeural",
                DisplayName = "Aria",
                SupportsSpeakingRate = true,
                SupportsPitch = true
            };

            // Act
            await _capabilityService.EnrichVoiceInfoAsync(voice);

            // Assert
            Assert.True(voice.SupportsStyle);
            Assert.True(voice.SupportsStyleDegree);
            Assert.True(voice.SupportsRole);
            Assert.NotEmpty(voice.AvailableStyles);
            Assert.NotEmpty(voice.AvailableRoles);
        }

        [Fact]
        public async Task EnrichVoiceInfo_WithUnsupportedVoice_NoStyleCapabilities()
        {
            // Arrange
            var voice = new VoiceInfo
            {
                Name = "en-GB-LibbyNeural",
                DisplayName = "Libby",
                SupportsSpeakingRate = true,
                SupportsPitch = true
            };

            // Act
            await _capabilityService.EnrichVoiceInfoAsync(voice);

            // Assert
            Assert.False(voice.SupportsStyle);
            Assert.False(voice.SupportsStyleDegree);
            Assert.False(voice.SupportsRole);
            Assert.Empty(voice.AvailableStyles);
            Assert.Empty(voice.AvailableRoles);
        }

        [Fact]
        public async Task GetVoiceCapabilities_CachesResults()
        {
            // Arrange
            var voiceName = "en-US-AriaNeural";

            // Act
            var capabilities1 = await _capabilityService.GetVoiceCapabilitiesAsync(voiceName);
            var capabilities2 = await _capabilityService.GetVoiceCapabilitiesAsync(voiceName);

            // Assert - Same instance means it was cached
            Assert.Same(capabilities1, capabilities2);
        }

        [Fact]
        public async Task GetVoiceCapabilities_AvailableStyles_IncludesCommonStyles()
        {
            // Arrange
            var voiceName = "en-US-AriaNeural";
            var expectedStyles = new[] { "cheerful", "sad", "angry", "neutral" };

            // Act
            var capabilities = await _capabilityService.GetVoiceCapabilitiesAsync(voiceName);

            // Assert
            Assert.NotNull(capabilities);
            foreach (var style in expectedStyles)
            {
                Assert.Contains(style, capabilities.AvailableStyles);
            }
        }

        [Fact]
        public async Task GetVoiceCapabilities_AvailableRoles_IncludesCommonRoles()
        {
            // Arrange
            var voiceName = "en-US-AriaNeural";
            var expectedRoles = new[] { "Girl", "Boy", "YoungAdultFemale", "YoungAdultMale" };

            // Act
            var capabilities = await _capabilityService.GetVoiceCapabilitiesAsync(voiceName);

            // Assert
            Assert.NotNull(capabilities);
            foreach (var role in expectedRoles)
            {
                Assert.Contains(role, capabilities.AvailableRoles);
            }
        }
    }
}
