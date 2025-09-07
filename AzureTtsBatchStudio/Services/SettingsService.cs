using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureTtsBatchStudio.Services
{
    public class AppSettings
    {
        public string AzureSubscriptionKey { get; set; } = string.Empty;
        public string AzureRegion { get; set; } = string.Empty;
        public string DefaultOutputDirectory { get; set; } = string.Empty;
        public string DefaultLanguage { get; set; } = "en-US";
        public string DefaultVoice { get; set; } = string.Empty;
        public double DefaultSpeakingRate { get; set; } = 1.0;
        public double DefaultPitch { get; set; } = 0.0;
        public string DefaultAudioFormat { get; set; } = "WAV";
        public string DefaultQuality { get; set; } = "Standard";
        public bool RememberLastSettings { get; set; } = true;
        public int MaxConcurrentProcessing { get; set; } = 3;
        public bool ShowProcessingDetails { get; set; } = true;
    }

    public interface ISettingsService
    {
        Task<AppSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(AppSettings settings);
        string GetSettingsFilePath();
    }

    public class SettingsService : ISettingsService
    {
        private readonly string _settingsDirectory;
        private readonly string _settingsFileName = "appsettings.json";

        public SettingsService()
        {
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AzureTtsBatchStudio"
            );

            // Ensure settings directory exists
            Directory.CreateDirectory(_settingsDirectory);
        }

        public string GetSettingsFilePath()
        {
            return Path.Combine(_settingsDirectory, _settingsFileName);
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            var settingsPath = GetSettingsFilePath();
            
            if (!File.Exists(settingsPath))
            {
                // Return default settings if file doesn't exist
                var defaultSettings = new AppSettings
                {
                    DefaultOutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TTS Output")
                };
                
                // Create default output directory
                Directory.CreateDirectory(defaultSettings.DefaultOutputDirectory);
                
                return defaultSettings;
            }

            try
            {
                var json = await File.ReadAllTextAsync(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                
                // Ensure output directory exists
                if (!string.IsNullOrEmpty(settings.DefaultOutputDirectory))
                {
                    Directory.CreateDirectory(settings.DefaultOutputDirectory);
                }
                
                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return new AppSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(settingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}