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
            Console.WriteLine("Starting application framework initialization...");
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Console.WriteLine("Classic desktop application lifetime detected.");
                
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                Console.WriteLine("Avalonia data annotation validation disabled.");
                
                // Load settings to apply theme
                Console.WriteLine("Loading application settings...");
                var settingsService = new SettingsService();
                var settings = await settingsService.LoadSettingsAsync();
                ApplyTheme(settings.ThemeVariant);
                Console.WriteLine($"Theme applied: {settings.ThemeVariant}");
                
                Console.WriteLine("Creating main window...");
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                
                desktop.MainWindow = mainWindow;
                Console.WriteLine("Main window created and assigned to desktop.MainWindow.");
                
                // Note: Avalonia automatically shows the MainWindow when assigned to desktop.MainWindow
                // No need to call mainWindow.Show() explicitly
            }
            else
            {
                Console.WriteLine("ERROR: Not running as classic desktop application.");
                Environment.Exit(1);
                return;
            }
        }
        catch (Exception ex)
        {
            // Log the error - in a real application you might want to use a proper logging framework
            Console.WriteLine($"CRITICAL ERROR during application initialization: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // If we can't initialize the main window, we should terminate gracefully
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Console.WriteLine("Shutting down application due to initialization error.");
                desktop.Shutdown(1);
            }
            return;
        }

        Console.WriteLine("Calling base framework initialization...");
        base.OnFrameworkInitializationCompleted();
        Console.WriteLine("Application framework initialization completed successfully.");
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