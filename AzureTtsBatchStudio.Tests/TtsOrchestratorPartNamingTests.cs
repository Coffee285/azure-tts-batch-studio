using System;
using System.IO;
using System.Reflection;
using AzureTtsBatchStudio.Tts;
using AzureTtsBatchStudio.Models;
using Xunit;

namespace AzureTtsBatchStudio.Tests
{
    public class TtsOrchestratorPartNamingTests
    {
        [Fact]
        public void CreatePartRequest_ShouldIncludeBaseFilenameInPartName()
        {
            // Arrange
            var baseRequest = new TtsRequest
            {
                OutputFileName = "/test/output/speech_20241215_143022.mp3",
                Voice = new VoiceInfo { Name = "test-voice" },
                Format = new AudioFormat { Name = "MP3" },
                Quality = new QualityOption { Name = "Standard" }
            };

            var part = new TtsPart
            {
                Index = 1,
                PlainText = "Test text",
                SafeSsml = "<speak>Test text</speak>"
            };

            // Act - Use reflection to call the private CreatePartRequest method
            var orchestratorType = typeof(TtsOrchestrator);
            var method = orchestratorType.GetMethod("CreatePartRequest", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            Assert.NotNull(method);
            
            var result = (TtsRequest)method.Invoke(null, new object[] { baseRequest, part, "/test/output" });

            // Assert
            var expectedPartName = "speech_20241215_143022_part_001.mp3";
            var actualPartName = Path.GetFileName(result.OutputFileName);
            
            Assert.Equal(expectedPartName, actualPartName);
            Assert.Equal("/test/output/speech_20241215_143022_part_001.mp3", result.OutputFileName);
        }

        [Fact] 
        public void CreatePartRequest_ShouldWorkWithDifferentExtensions()
        {
            // Arrange
            var baseRequest = new TtsRequest
            {
                OutputFileName = "/test/output/voice_20241215_150000.wav",
                Voice = new VoiceInfo { Name = "test-voice" },
                Format = new AudioFormat { Name = "WAV" },
                Quality = new QualityOption { Name = "Standard" }
            };

            var part = new TtsPart
            {
                Index = 5,
                PlainText = "Test text",
                SafeSsml = "<speak>Test text</speak>"
            };

            // Act
            var orchestratorType = typeof(TtsOrchestrator);
            var method = orchestratorType.GetMethod("CreatePartRequest", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            var result = (TtsRequest)method.Invoke(null, new object[] { baseRequest, part, "/test/output" });

            // Assert
            var expectedPartName = "voice_20241215_150000_part_005.wav";
            var actualPartName = Path.GetFileName(result.OutputFileName);
            
            Assert.Equal(expectedPartName, actualPartName);
        }
    }
}