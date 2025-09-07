using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureTtsBatchStudio.Models;
using AzureTtsBatchStudio.Services;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace AzureTtsBatchStudio.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IAzureTtsService _ttsService;
        private readonly ISettingsService _settingsService;
        private AppSettings _currentSettings = new();
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private string _inputText = "Welcome to Azure TTS Batch Studio! This is a sample text to convert to speech.";

        [ObservableProperty]
        private ObservableCollection<LanguageInfo> _availableLanguages = new();

        [ObservableProperty]
        private ObservableCollection<VoiceInfo> _availableVoices = new();

        [ObservableProperty]
        private LanguageInfo? _selectedLanguage;

        [ObservableProperty]
        private VoiceInfo? _selectedVoice;

        [ObservableProperty]
        private double _speakingRate = 1.0;

        [ObservableProperty]
        private double _pitch = 0.0;

        [ObservableProperty]
        private ObservableCollection<string> _audioFormats = new() { "WAV", "MP3", "OGG" };

        [ObservableProperty]
        private string _selectedAudioFormat = "WAV";

        [ObservableProperty]
        private ObservableCollection<string> _qualityOptions = new() { "Standard", "High", "Premium" };

        [ObservableProperty]
        private string _selectedQuality = "Standard";

        [ObservableProperty]
        private string _outputDirectory = "";

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private double _progressValue = 0;

        [ObservableProperty]
        private int _processedCount = 0;

        [ObservableProperty]
        private int _totalCount = 0;

        public MainWindowViewModel() : this(new AzureTtsService(), new SettingsService())
        {
        }

        public MainWindowViewModel(IAzureTtsService ttsService, ISettingsService settingsService)
        {
            _ttsService = ttsService;
            _settingsService = settingsService;
            
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadSettingsAsync();
            await LoadAvailableLanguagesAsync();
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                _currentSettings = await _settingsService.LoadSettingsAsync();
                OutputDirectory = _currentSettings.DefaultOutputDirectory;
                SpeakingRate = _currentSettings.DefaultSpeakingRate;
                Pitch = _currentSettings.DefaultPitch;
                SelectedAudioFormat = _currentSettings.DefaultAudioFormat;
                SelectedQuality = _currentSettings.DefaultQuality;

                if (!string.IsNullOrEmpty(_currentSettings.AzureSubscriptionKey) && 
                    !string.IsNullOrEmpty(_currentSettings.AzureRegion))
                {
                    _ttsService.ConfigureConnection(_currentSettings.AzureSubscriptionKey, _currentSettings.AzureRegion);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
            }
        }

        private async Task LoadAvailableLanguagesAsync()
        {
            try
            {
                if (!_ttsService.IsConfigured)
                {
                    StatusMessage = "Azure TTS not configured. Please check settings.";
                    return;
                }

                StatusMessage = "Loading available languages...";
                var languages = await _ttsService.GetAvailableLanguagesAsync();
                
                AvailableLanguages.Clear();
                foreach (var language in languages)
                {
                    AvailableLanguages.Add(language);
                }

                // Set default language
                SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == _currentSettings.DefaultLanguage) 
                                   ?? AvailableLanguages.FirstOrDefault();

                StatusMessage = $"Loaded {languages.Count} languages";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading languages: {ex.Message}";
            }
        }

        partial void OnSelectedLanguageChanged(LanguageInfo? value)
        {
            if (value != null)
            {
                _ = LoadVoicesForLanguageAsync(value.Code);
            }
        }

        private async Task LoadVoicesForLanguageAsync(string locale)
        {
            try
            {
                if (!_ttsService.IsConfigured)
                    return;

                StatusMessage = $"Loading voices for {locale}...";
                var voices = await _ttsService.GetAvailableVoicesAsync(locale);
                
                AvailableVoices.Clear();
                foreach (var voice in voices)
                {
                    AvailableVoices.Add(voice);
                }

                // Set default voice
                SelectedVoice = AvailableVoices.FirstOrDefault(v => v.Name == _currentSettings.DefaultVoice) 
                                ?? AvailableVoices.FirstOrDefault();

                StatusMessage = $"Loaded {voices.Count} voices for {locale}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading voices: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadTextFile()
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var storageProvider = desktop.MainWindow?.StorageProvider;
                    if (storageProvider == null) return;

                    var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Select Text File",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt", "*.md" } },
                            new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                        }
                    });

                    if (files.Count > 0)
                    {
                        var content = await File.ReadAllTextAsync(files[0].Path.LocalPath);
                        InputText = content;
                        StatusMessage = $"Loaded text file: {Path.GetFileName(files[0].Path.LocalPath)}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading file: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ImportCsv()
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var storageProvider = desktop.MainWindow?.StorageProvider;
                    if (storageProvider == null) return;

                    var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Select CSV File",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                            new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                        }
                    });

                    if (files.Count > 0)
                    {
                        var lines = await File.ReadAllLinesAsync(files[0].Path.LocalPath);
                        var content = string.Join("\n", lines);
                        InputText = content;
                        StatusMessage = $"Imported CSV file: {Path.GetFileName(files[0].Path.LocalPath)}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing CSV: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ClearText()
        {
            InputText = "";
            StatusMessage = "Text cleared";
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
                        Title = "Select Output Directory",
                        AllowMultiple = false
                    });

                    if (folders.Count > 0)
                    {
                        OutputDirectory = folders[0].Path.LocalPath;
                        StatusMessage = $"Output directory set to: {OutputDirectory}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error selecting directory: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task GenerateSpeech()
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                StatusMessage = "Please enter text to convert";
                return;
            }

            if (SelectedVoice == null)
            {
                StatusMessage = "Please select a voice";
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                StatusMessage = "Please select an output directory";
                return;
            }

            try
            {
                IsProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();

                var requests = CreateTtsRequests();
                TotalCount = requests.Count;
                ProcessedCount = 0;

                var progress = new Progress<ProcessingProgress>(OnProgressUpdated);

                StatusMessage = "Starting speech generation...";
                
                var success = await _ttsService.GenerateBatchSpeechAsync(
                    requests, 
                    _currentSettings.AzureSubscriptionKey, 
                    _currentSettings.AzureRegion,
                    progress,
                    _cancellationTokenSource.Token);

                if (success)
                {
                    StatusMessage = "Speech generation completed successfully";
                }
                else
                {
                    StatusMessage = "Speech generation completed with errors";
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Speech generation cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating speech: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private async Task PreviewVoice()
        {
            if (SelectedVoice == null)
            {
                StatusMessage = "Please select a voice to preview";
                return;
            }

            try
            {
                var previewText = "Hello! This is a preview of the selected voice.";
                var tempFile = Path.Combine(Path.GetTempPath(), $"voice_preview_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

                var request = new TtsRequest
                {
                    Text = previewText,
                    OutputFileName = tempFile,
                    Voice = SelectedVoice,
                    SpeakingRate = SpeakingRate,
                    Pitch = Pitch,
                    Format = new AudioFormat { Name = "WAV", Extension = ".wav" },
                    Quality = new QualityOption { Name = "Standard" }
                };

                StatusMessage = "Generating voice preview...";
                var success = await _ttsService.GenerateSpeechAsync(
                    request, 
                    _currentSettings.AzureSubscriptionKey, 
                    _currentSettings.AzureRegion);

                if (success)
                {
                    StatusMessage = "Voice preview generated. Check your system's default audio player.";
                    // Note: In a real implementation, you'd want to play the audio directly
                }
                else
                {
                    StatusMessage = "Failed to generate voice preview";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating preview: {ex.Message}";
            }
        }

        [RelayCommand]
        private void StopProcessing()
        {
            _cancellationTokenSource?.Cancel();
            StatusMessage = "Stopping processing...";
        }

        [RelayCommand]
        private async Task OpenSettings()
        {
            try
            {
                var settingsWindow = new Views.SettingsWindow();
                var settingsViewModel = new SettingsWindowViewModel(_ttsService, _settingsService);
                settingsWindow.DataContext = settingsViewModel;

                // Handle settings saved event
                settingsViewModel.SettingsSaved += async (sender, e) =>
                {
                    await LoadSettingsAsync();
                    await LoadAvailableLanguagesAsync();
                    settingsWindow.Close();
                    StatusMessage = "Settings saved successfully";
                };

                // Handle cancelled event
                settingsViewModel.Cancelled += (sender, e) =>
                {
                    settingsWindow.Close();
                };

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    await settingsWindow.ShowDialog(desktop.MainWindow);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening settings: {ex.Message}";
            }
        }

        private List<TtsRequest> CreateTtsRequests()
        {
            var requests = new List<TtsRequest>();
            
            if (string.IsNullOrWhiteSpace(InputText) || SelectedVoice == null)
                return requests;

            // Split text into paragraphs or sentences for batch processing
            var paragraphs = InputText.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            if (paragraphs.Length == 1)
            {
                // Single text block
                var fileName = $"speech_{DateTime.Now:yyyyMMdd_HHmmss}.{GetFileExtension()}";
                var outputPath = Path.Combine(OutputDirectory, fileName);

                requests.Add(new TtsRequest
                {
                    Text = InputText.Trim(),
                    OutputFileName = outputPath,
                    Voice = SelectedVoice,
                    SpeakingRate = SpeakingRate,
                    Pitch = Pitch,
                    Format = CreateAudioFormat(),
                    Quality = CreateQualityOption()
                });
            }
            else
            {
                // Multiple paragraphs
                for (int i = 0; i < paragraphs.Length; i++)
                {
                    var paragraph = paragraphs[i].Trim();
                    if (string.IsNullOrWhiteSpace(paragraph)) continue;

                    var fileName = $"speech_part_{i + 1:D3}_{DateTime.Now:yyyyMMdd_HHmmss}.{GetFileExtension()}";
                    var outputPath = Path.Combine(OutputDirectory, fileName);

                    requests.Add(new TtsRequest
                    {
                        Text = paragraph,
                        OutputFileName = outputPath,
                        Voice = SelectedVoice,
                        SpeakingRate = SpeakingRate,
                        Pitch = Pitch,
                        Format = CreateAudioFormat(),
                        Quality = CreateQualityOption()
                    });
                }
            }

            return requests;
        }

        private AudioFormat CreateAudioFormat()
        {
            return SelectedAudioFormat switch
            {
                "MP3" => new AudioFormat { Name = "MP3", Extension = ".mp3", MimeType = "audio/mpeg" },
                "OGG" => new AudioFormat { Name = "OGG", Extension = ".ogg", MimeType = "audio/ogg" },
                _ => new AudioFormat { Name = "WAV", Extension = ".wav", MimeType = "audio/wav" }
            };
        }

        private QualityOption CreateQualityOption()
        {
            return SelectedQuality switch
            {
                "High" => new QualityOption { Name = "High", BitRate = 128, SampleRate = 44100 },
                "Premium" => new QualityOption { Name = "Premium", BitRate = 320, SampleRate = 48000 },
                _ => new QualityOption { Name = "Standard", BitRate = 64, SampleRate = 22050 }
            };
        }

        private string GetFileExtension()
        {
            return SelectedAudioFormat.ToLower() switch
            {
                "mp3" => "mp3",
                "ogg" => "ogg",
                _ => "wav"
            };
        }

        private void OnProgressUpdated(ProcessingProgress progress)
        {
            ProcessedCount = progress.ProcessedItems;
            TotalCount = progress.TotalItems;
            StatusMessage = progress.Status;
            
            if (progress.TotalItems > 0)
            {
                ProgressValue = (double)progress.ProcessedItems / progress.TotalItems * 100;
            }
        }
    }
}
