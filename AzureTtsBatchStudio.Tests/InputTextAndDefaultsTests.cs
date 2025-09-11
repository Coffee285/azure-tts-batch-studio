using System;
using System.Threading.Tasks;
using Xunit;
using AzureTtsBatchStudio.ViewModels;
using AzureTtsBatchStudio.Services;
using AzureTtsBatchStudio.Models;
using System.Collections.Generic;

namespace AzureTtsBatchStudio.Tests
{
    public class MockTtsService : IAzureTtsService
    {
        public bool IsConfigured => true;
        
        public void ConfigureConnection(string subscriptionKey, string region) { }
        
        public Task<List<LanguageInfo>> GetAvailableLanguagesAsync() => 
            Task.FromResult(new List<LanguageInfo>());
            
        public Task<List<VoiceInfo>> GetAvailableVoicesAsync(string? locale = null) => 
            Task.FromResult(new List<VoiceInfo>());
            
        public Task<bool> TestConnectionAsync(string subscriptionKey, string region) => 
            Task.FromResult(true);
            
        public Task<bool> GenerateSpeechAsync(TtsRequest request, string subscriptionKey, string region, CancellationToken cancellationToken = default) => 
            Task.FromResult(true);
            
        public Task<bool> GenerateBatchSpeechAsync(List<TtsRequest> requests, string subscriptionKey, string region, 
            IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default) => 
            Task.FromResult(true);
    }
    
    public class MockSettingsService : ISettingsService
    {
        private readonly AppSettings _settings;
        
        public MockSettingsService(AppSettings? customSettings = null)
        {
            _settings = customSettings ?? new AppSettings();
        }
        
        public Task<AppSettings> LoadSettingsAsync() => 
            Task.FromResult(_settings);
            
        public Task SaveSettingsAsync(AppSettings settings) => 
            Task.CompletedTask;
            
        public string GetSettingsFilePath() => "/tmp/test_settings.json";
    }

    public class InputTextAndDefaultsTests
    {
        [Fact]
        public void MainWindowViewModel_Should_Initialize_With_Empty_InputText()
        {
            // Arrange & Act
            var viewModel = new MainWindowViewModel(new MockTtsService(), new MockSettingsService());
            
            // Assert
            Assert.Equal("", viewModel.InputText);
        }
        
        [Fact]
        public void MainWindowViewModel_Should_Initialize_With_Correct_Default_Pitch_And_Speed()
        {
            // Arrange & Act
            var viewModel = new MainWindowViewModel(new MockTtsService(), new MockSettingsService());
            
            // Assert
            Assert.Equal(0.0, viewModel.Pitch); // Default pitch should be 0
            Assert.Equal(1.0, viewModel.SpeakingRate); // Default speaking rate should be 1.0 (100%)
        }
        
        [Fact]
        public async Task MainWindowViewModel_Should_Use_Settings_Default_Values_When_Available()
        {
            // Arrange
            var customSettings = new AppSettings
            {
                DefaultPitch = 0.0,
                DefaultSpeakingRate = 1.0
            };
            var settingsService = new MockSettingsService(customSettings);
            var viewModel = new MainWindowViewModel(new MockTtsService(), settingsService);
            
            // Wait for async initialization
            await Task.Delay(100);
            
            // Assert
            Assert.Equal(0.0, viewModel.Pitch);
            Assert.Equal(1.0, viewModel.SpeakingRate);
        }
        
        [Fact]
        public void InputText_Property_Should_Be_Modifiable()
        {
            // Arrange
            var viewModel = new MainWindowViewModel(new MockTtsService(), new MockSettingsService());
            var testText = "This is a test text for TTS conversion";
            
            // Act
            viewModel.InputText = testText;
            
            // Assert
            Assert.Equal(testText, viewModel.InputText);
        }
        
        [Fact]
        public void SettingsService_Should_Return_Correct_Defaults_When_No_File_Exists()
        {
            // Arrange
            var settingsService = new SettingsService();
            
            // Act
            var settings = settingsService.LoadSettingsAsync().Result;
            
            // Assert
            Assert.Equal(1.0, settings.DefaultSpeakingRate);
            Assert.Equal(0.0, settings.DefaultPitch);
        }
    }
}