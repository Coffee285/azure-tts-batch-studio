using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using AzureTtsBatchStudio.ViewModels;
using AzureTtsBatchStudio.Views;
using AzureTtsBatchStudio.Services;
using Avalonia.Styling;

namespace AzureTtsBatchStudio;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                
                // Load settings to apply theme
                var settingsService = new SettingsService();
                var settings = await settingsService.LoadSettingsAsync();
                ApplyTheme(settings.ThemeVariant);
                
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }
        }
        catch (Exception ex)
        {
            // Log the error - in a real application you might want to use a proper logging framework
            Console.WriteLine($"Error during application initialization: {ex.Message}");
            
            // If we can't initialize the main window, we should terminate gracefully
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(1);
            }
            return;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void ApplyTheme(string themeVariant)
    {
        RequestedThemeVariant = themeVariant switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}