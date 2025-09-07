using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureTtsBatchStudio.Models;
using AzureTtsBatchStudio.Services;
using Avalonia.Platform.Storage;

namespace AzureTtsBatchStudio.ViewModels
{
    public partial class SettingsWindowViewModel : ViewModelBase
    {
        private readonly IAzureTtsService _ttsService;
        private readonly ISettingsService _settingsService;
        private AppSettings _originalSettings = new();

        [ObservableProperty]
        private string _subscriptionKey = string.Empty;

        [ObservableProperty]
        private string _region = string.Empty;

        [ObservableProperty]
        private string _connectionStatus = "Not tested";

        [ObservableProperty]
        private IBrush _connectionStatusColor = Brushes.Gray;

        [ObservableProperty]
        private string _defaultOutputDirectory = string.Empty;

        [ObservableProperty]
        private ObservableCollection<LanguageInfo> _availableLanguages = new();

        [ObservableProperty]
        private LanguageInfo? _defaultLanguage;

        [ObservableProperty]
        private ObservableCollection<string> _audioFormats = new() { "WAV", "MP3", "OGG" };

        [ObservableProperty]
        private string _defaultAudioFormat = "WAV";

        [ObservableProperty]
        private ObservableCollection<string> _qualityOptions = new() { "Standard", "High", "Premium" };

        [ObservableProperty]
        private string _defaultQuality = "Standard";

        [ObservableProperty]
        private double _defaultSpeakingRate = 1.0;

        [ObservableProperty]
        private double _defaultPitch = 0.0;

        [ObservableProperty]
        private bool _rememberLastSettings = true;

        [ObservableProperty]
        private int _maxConcurrentProcessing = 3;

        [ObservableProperty]
        private bool _showProcessingDetails = true;

        [ObservableProperty]
        private bool _isTesting = false;

        // UI Preferences
        [ObservableProperty]
        private ObservableCollection<string> _themeOptions = new() { "Light", "Dark", "Default" };

        [ObservableProperty]
        private string _selectedTheme = "Default";

        [ObservableProperty]
        private ObservableCollection<string> _fontSizeOptions = new() { "Small", "Medium", "Large" };

        [ObservableProperty]
        private string _selectedFontSize = "Medium";

        [ObservableProperty]
        private ObservableCollection<string> _fontFamilyOptions = new() { "Segoe UI", "Calibri", "Arial", "Consolas" };

        [ObservableProperty]
        private string _selectedFontFamily = "Segoe UI";

        [ObservableProperty]
        private ObservableCollection<string> _layoutStyleOptions = new() { "Compact", "Standard", "Spacious" };

        [ObservableProperty]
        private string _selectedLayoutStyle = "Standard";

        public event EventHandler? SettingsSaved;
        public event EventHandler? Cancelled;

        public SettingsWindowViewModel() : this(new AzureTtsService(), new SettingsService())
        {
        }

        public SettingsWindowViewModel(IAzureTtsService ttsService, ISettingsService settingsService)
        {
            _ttsService = ttsService;
            _settingsService = settingsService;
            
            _ = LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                _originalSettings = await _settingsService.LoadSettingsAsync();
                
                // Populate UI with current settings
                SubscriptionKey = _originalSettings.AzureSubscriptionKey;
                Region = _originalSettings.AzureRegion;
                DefaultOutputDirectory = _originalSettings.DefaultOutputDirectory;
                DefaultAudioFormat = _originalSettings.DefaultAudioFormat;
                DefaultQuality = _originalSettings.DefaultQuality;
                DefaultSpeakingRate = _originalSettings.DefaultSpeakingRate;
                DefaultPitch = _originalSettings.DefaultPitch;
                RememberLastSettings = _originalSettings.RememberLastSettings;
                MaxConcurrentProcessing = _originalSettings.MaxConcurrentProcessing;
                ShowProcessingDetails = _originalSettings.ShowProcessingDetails;
                
                // Load UI preferences
                SelectedTheme = _originalSettings.ThemeVariant;
                SelectedFontSize = _originalSettings.FontSize;
                SelectedFontFamily = _originalSettings.FontFamily;
                SelectedLayoutStyle = _originalSettings.LayoutStyle;

                // Load available languages if credentials are configured
                if (!string.IsNullOrEmpty(SubscriptionKey) && !string.IsNullOrEmpty(Region))
                {
                    await LoadAvailableLanguagesAsync();
                }
                else
                {
                    // Add some default languages
                    AddDefaultLanguages();
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error loading settings: {ex.Message}";
                ConnectionStatusColor = Brushes.Red;
            }
        }

        private void AddDefaultLanguages()
        {
            var defaultLanguages = new List<LanguageInfo>
            {
                new() { Code = "en-US", Name = "en-US", DisplayName = "English (United States)" },
                new() { Code = "en-GB", Name = "en-GB", DisplayName = "English (United Kingdom)" },
                new() { Code = "es-ES", Name = "es-ES", DisplayName = "Spanish (Spain)" },
                new() { Code = "fr-FR", Name = "fr-FR", DisplayName = "French (France)" },
                new() { Code = "de-DE", Name = "de-DE", DisplayName = "German (Germany)" },
                new() { Code = "it-IT", Name = "it-IT", DisplayName = "Italian (Italy)" },
                new() { Code = "pt-BR", Name = "pt-BR", DisplayName = "Portuguese (Brazil)" },
                new() { Code = "ja-JP", Name = "ja-JP", DisplayName = "Japanese (Japan)" },
                new() { Code = "ko-KR", Name = "ko-KR", DisplayName = "Korean (Korea)" },
                new() { Code = "zh-CN", Name = "zh-CN", DisplayName = "Chinese (Mandarin, Simplified)" }
            };

            AvailableLanguages.Clear();
            foreach (var language in defaultLanguages)
            {
                AvailableLanguages.Add(language);
            }

            DefaultLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == _originalSettings.DefaultLanguage) 
                              ?? AvailableLanguages.FirstOrDefault();
        }

        private async Task LoadAvailableLanguagesAsync()
        {
            try
            {
                _ttsService.ConfigureConnection(SubscriptionKey, Region);
                var languages = await _ttsService.GetAvailableLanguagesAsync();
                
                AvailableLanguages.Clear();
                foreach (var language in languages)
                {
                    AvailableLanguages.Add(language);
                }

                DefaultLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == _originalSettings.DefaultLanguage) 
                                  ?? AvailableLanguages.FirstOrDefault();
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error loading languages: {ex.Message}";
                ConnectionStatusColor = Brushes.Red;
                
                // Fall back to default languages
                AddDefaultLanguages();
            }
        }

        [RelayCommand]
        private async Task TestConnection()
        {
            if (string.IsNullOrWhiteSpace(SubscriptionKey) || string.IsNullOrWhiteSpace(Region))
            {
                ConnectionStatus = "Please enter subscription key and region";
                ConnectionStatusColor = Brushes.Orange;
                return;
            }

            try
            {
                IsTesting = true;
                ConnectionStatus = "Testing connection...";
                ConnectionStatusColor = Brushes.Blue;

                var success = await _ttsService.TestConnectionAsync(SubscriptionKey.Trim(), Region.Trim());
                
                if (success)
                {
                    ConnectionStatus = "Connection successful!";
                    ConnectionStatusColor = Brushes.Green;
                    
                    // Load available languages after successful connection
                    await LoadAvailableLanguagesAsync();
                }
                else
                {
                    ConnectionStatus = "Connection failed. Please check your credentials.";
                    ConnectionStatusColor = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Connection error: {ex.Message}";
                ConnectionStatusColor = Brushes.Red;
            }
            finally
            {
                IsTesting = false;
            }
        }

        [RelayCommand]
        private async Task BrowseOutputDirectory()
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var storageProvider = desktop.MainWindow?.StorageProvider;
                    if (storageProvider == null) return;

                    var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                    {
                        Title = "Select Default Output Directory",
                        AllowMultiple = false
                    });

                    if (folders.Count > 0)
                    {
                        DefaultOutputDirectory = folders[0].Path.LocalPath;
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error selecting directory: {ex.Message}";
                ConnectionStatusColor = Brushes.Red;
            }
        }

        [RelayCommand]
        private void RestoreDefaults()
        {
            SubscriptionKey = string.Empty;
            Region = string.Empty;
            DefaultOutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TTS Output");
            DefaultAudioFormat = "WAV";
            DefaultQuality = "Standard";
            DefaultSpeakingRate = 1.0;
            DefaultPitch = 0.0;
            RememberLastSettings = true;
            MaxConcurrentProcessing = 3;
            ShowProcessingDetails = true;
            
            ConnectionStatus = "Settings restored to defaults";
            ConnectionStatusColor = Brushes.Green;
        }

        [RelayCommand]
        private async Task Save()
        {
            try
            {
                var settings = new AppSettings
                {
                    AzureSubscriptionKey = SubscriptionKey.Trim(),
                    AzureRegion = Region.Trim(),
                    DefaultOutputDirectory = DefaultOutputDirectory,
                    DefaultLanguage = DefaultLanguage?.Code ?? "en-US",
                    DefaultAudioFormat = DefaultAudioFormat,
                    DefaultQuality = DefaultQuality,
                    DefaultSpeakingRate = DefaultSpeakingRate,
                    DefaultPitch = DefaultPitch,
                    RememberLastSettings = RememberLastSettings,
                    MaxConcurrentProcessing = MaxConcurrentProcessing,
                    ShowProcessingDetails = ShowProcessingDetails,
                    // UI Preferences
                    ThemeVariant = SelectedTheme,
                    FontSize = SelectedFontSize,
                    FontFamily = SelectedFontFamily,
                    LayoutStyle = SelectedLayoutStyle
                };

                await _settingsService.SaveSettingsAsync(settings);
                
                // Apply theme immediately
                if (Application.Current is App app)
                {
                    app.ApplyTheme(settings.ThemeVariant);
                }
                
                // Configure the TTS service with new credentials
                if (!string.IsNullOrEmpty(settings.AzureSubscriptionKey) && !string.IsNullOrEmpty(settings.AzureRegion))
                {
                    _ttsService.ConfigureConnection(settings.AzureSubscriptionKey, settings.AzureRegion);
                }

                SettingsSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error saving settings: {ex.Message}";
                ConnectionStatusColor = Brushes.Red;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}