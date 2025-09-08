using System;
using System.Threading.Tasks;
using AzureTtsBatchStudio.ViewModels;
using AzureTtsBatchStudio.Services;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Testing MainWindowViewModel initialization...");
        
        // Create the view model which should trigger initialization
        var settingsService = new SettingsService();
        var ttsService = new AzureTtsService();
        
        var viewModel = new MainWindowViewModel(ttsService, settingsService);
        
        // Wait a bit for the async initialization to complete
        await Task.Delay(5000);
        
        Console.WriteLine($"Available Languages Count: {viewModel.AvailableLanguages.Count}");
        foreach (var lang in viewModel.AvailableLanguages)
        {
            Console.WriteLine($"  - {lang.Code}: {lang.DisplayName}");
        }
        
        Console.WriteLine($"Available Voices Count: {viewModel.AvailableVoices.Count}");
        foreach (var voice in viewModel.AvailableVoices)
        {
            Console.WriteLine($"  - {voice.Name}: {voice.DisplayName} ({voice.Gender})");
        }
        
        Console.WriteLine($"Selected Language: {viewModel.SelectedLanguage?.DisplayName ?? "null"}");
        Console.WriteLine($"Selected Voice: {viewModel.SelectedVoice?.DisplayName ?? "null"}");
        Console.WriteLine($"Status Message: {viewModel.StatusMessage}");
        Console.WriteLine($"TTS Service Configured: {ttsService.IsConfigured}");
    }
}