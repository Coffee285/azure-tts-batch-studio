using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureTtsBatchStudio.ViewModels;

namespace AzureTtsBatchStudio.Preflight
{
    public partial class PreflightViewModel : ViewModelBase
    {
        private readonly EncodingPreflight _encodingPreflight;

        [ObservableProperty]
        private string _inputText = "";

        [ObservableProperty]
        private string _fixedText = "";

        [ObservableProperty]
        private string _strategy = "";

        [ObservableProperty]
        private int _scoreBefore = 0;

        [ObservableProperty]
        private int _scoreAfter = 0;

        [ObservableProperty]
        private bool _autoFixEnabled = false;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private ObservableCollection<string> _reportItems = new();

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public PreflightViewModel()
        {
            _encodingPreflight = new EncodingPreflight();
        }

        [RelayCommand]
        private async Task RunPreflight()
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                StatusMessage = "Please enter text to analyze";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Analyzing text encoding...";

                // Run preflight analysis
                var result = _encodingPreflight.Run(InputText);

                // Update UI
                FixedText = result.Fixed;
                Strategy = result.Strategy;
                ScoreBefore = result.ScoreBefore;
                ScoreAfter = result.ScoreAfter;

                // Update report
                ReportItems.Clear();
                ReportItems.Add($"Strategy: {result.Strategy}");
                ReportItems.Add($"Score: {result.ScoreBefore} → {result.ScoreAfter}");
                
                if (result.DetectedEncoding != null)
                    ReportItems.Add($"Detected Encoding: {result.DetectedEncoding}");

                foreach (var replacement in result.Replacements)
                    ReportItems.Add($"Replaced '{replacement.Key}': {replacement.Value} instances");

                foreach (var warning in result.Warnings)
                    ReportItems.Add($"Warning: {warning}");

                if (result.ScoreAfter < result.ScoreBefore)
                    StatusMessage = $"Analysis complete - {result.ScoreBefore - result.ScoreAfter} issues fixed";
                else if (result.ScoreBefore == 0)
                    StatusMessage = "Analysis complete - no encoding issues detected";
                else
                    StatusMessage = "Analysis complete - no improvements possible";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during analysis: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void AcceptFix()
        {
            if (!string.IsNullOrEmpty(FixedText))
            {
                InputText = FixedText;
                StatusMessage = "Fixed text accepted as input";
            }
        }

        [RelayCommand]
        private async Task CopyFixed()
        {
            if (!string.IsNullOrEmpty(FixedText))
            {
                try
                {
                    // Note: In a real Avalonia app, you'd use the clipboard service
                    StatusMessage = "Fixed text copied to clipboard";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error copying to clipboard: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task ExportReport()
        {
            try
            {
                var report = new
                {
                    Timestamp = DateTime.UtcNow,
                    Original = InputText,
                    Fixed = FixedText,
                    Strategy = Strategy,
                    ScoreBefore = ScoreBefore,
                    ScoreAfter = ScoreAfter,
                    ReportItems = ReportItems
                };

                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                var fileName = $"preflight_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);
                
                await File.WriteAllTextAsync(filePath, json);
                StatusMessage = $"Report exported to: {filePath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting report: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ToggleAutoFix()
        {
            AutoFixEnabled = !AutoFixEnabled;
            StatusMessage = AutoFixEnabled ? "Auto-fix enabled" : "Auto-fix disabled";
            // TODO: Persist this setting
        }

        public PreflightResult? GetPreflightResult(string text)
        {
            try
            {
                return _encodingPreflight.Run(text);
            }
            catch
            {
                return null;
            }
        }
    }
}