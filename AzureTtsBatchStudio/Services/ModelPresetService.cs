using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    public interface IModelPresetService
    {
        Task<List<ModelPreset>> GetPresetsAsync();
        Task SavePresetsAsync(List<ModelPreset> presets);
        Task<ModelPreset?> GetPresetByIdAsync(string id);
        string GetModelsConfigPath();
    }

    public class ModelPresetService : IModelPresetService
    {
        private const string ConfigFileName = "models.json";
        private readonly string _configPath;

        public ModelPresetService()
        {
            // Place config in repo root/config directory
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "config", ConfigFileName);
        }

        public string GetModelsConfigPath()
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return _configPath;
        }

        public async Task<List<ModelPreset>> GetPresetsAsync()
        {
            try
            {
                var configPath = GetModelsConfigPath();
                if (!File.Exists(configPath))
                {
                    // Create default config if it doesn't exist
                    var defaultPresets = GetDefaultPresets();
                    await SavePresetsAsync(defaultPresets);
                    return defaultPresets;
                }

                var json = await File.ReadAllTextAsync(configPath);
                var config = JsonSerializer.Deserialize<ModelPresetsConfig>(json);
                return config?.Presets ?? GetDefaultPresets();
            }
            catch (Exception)
            {
                // Return defaults if there's any error reading the config
                return GetDefaultPresets();
            }
        }

        public async Task SavePresetsAsync(List<ModelPreset> presets)
        {
            try
            {
                var config = new ModelPresetsConfig { Presets = presets };
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                
                var configPath = GetModelsConfigPath();
                await File.WriteAllTextAsync(configPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save model presets: {ex.Message}", ex);
            }
        }

        public async Task<ModelPreset?> GetPresetByIdAsync(string id)
        {
            var presets = await GetPresetsAsync();
            return presets.FirstOrDefault(p => p.Id == id);
        }

        private static List<ModelPreset> GetDefaultPresets()
        {
            return new List<ModelPreset>
            {
                new() { Label = "Fast (Mini)", Id = "gpt-4o-mini", Notes = "Lower cost; fast drafting" },
                new() { Label = "Quality (4o)", Id = "gpt-4o", Notes = "Higher quality; slower" },
                new() { Label = "Legacy (4)", Id = "gpt-4", Notes = "Previous generation model" }
            };
        }
    }
}