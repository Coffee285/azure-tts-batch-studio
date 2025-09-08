using Avalonia;
using Avalonia.Controls;
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
using System.Threading.Tasks;

namespace AzureTtsBatchStudio;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
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
                
                // Initialize settings and theme synchronously with fallback
                Console.WriteLine("Loading application settings...");
                AppSettings settings;
                try
                {
                    var settingsService = new SettingsService();
                    // Load settings synchronously to avoid async void issues
                    settings = Task.Run(async () => await settingsService.LoadSettingsAsync()).GetAwaiter().GetResult();
                    Console.WriteLine($"Settings loaded successfully. Theme: {settings.ThemeVariant}");
                }
                catch (Exception settingsEx)
                {
                    Console.WriteLine($"Warning: Failed to load settings, using defaults. Error: {settingsEx.Message}");
                    settings = new AppSettings(); // Use default settings
                }
                
                try
                {
                    ApplyTheme(settings.ThemeVariant);
                    Console.WriteLine($"Theme applied: {settings.ThemeVariant}");
                }
                catch (Exception themeEx)
                {
                    Console.WriteLine($"Warning: Failed to apply theme, using default. Error: {themeEx.Message}");
                    ApplyTheme("Light"); // Fallback to light theme
                }
                
                Console.WriteLine("Creating main window...");
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                
                // Explicitly set window properties to ensure visibility
                mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                mainWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen;
                mainWindow.Width = 1200;
                mainWindow.Height = 800;
                mainWindow.Topmost = false;
                
                desktop.MainWindow = mainWindow;
                Console.WriteLine("Main window created and assigned to desktop.MainWindow.");
                
                // Call base initialization first
                base.OnFrameworkInitializationCompleted();
                
                // Explicitly show the window to ensure it's visible
                Console.WriteLine("Explicitly showing main window...");
                mainWindow.Show();
                Console.WriteLine("Main window shown successfully.");
                
                // Try to activate and focus the window
                try
                {
                    mainWindow.Activate();
                    mainWindow.BringIntoView();
                    Console.WriteLine("Main window activated and brought into view.");
                }
                catch (Exception activateEx)
                {
                    Console.WriteLine($"Warning: Failed to activate window: {activateEx.Message}");
                }
                
                // Additional attempt to ensure window is visible
                try
                {
                    mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                    if (mainWindow.IsVisible == false)
                    {
                        Console.WriteLine("Window not visible, attempting to force visibility...");
                        mainWindow.Show();
                    }
                    Console.WriteLine($"Final window state - Visible: {mainWindow.IsVisible}, State: {mainWindow.WindowState}");
                }
                catch (Exception visibilityEx)
                {
                    Console.WriteLine($"Warning: Failed to ensure window visibility: {visibilityEx.Message}");
                }
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
            
            // Show error dialog if possible
            try
            {
                Console.WriteLine("Attempting to show error dialog...");
                // For now, just log the error clearly to console - we can't create UI in this context
                Console.WriteLine("╔══════════════════════════════════════════════════════════════");
                Console.WriteLine("║ CRITICAL APPLICATION ERROR");
                Console.WriteLine("╠══════════════════════════════════════════════════════════════");
                Console.WriteLine($"║ Error: {ex.Message}");
                Console.WriteLine("║");
                Console.WriteLine("║ This error prevented the application window from displaying.");
                Console.WriteLine("║ Please report this error with the details above.");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════");
            }
            catch
            {
                // If we can't show a message box, just log to console
                Console.WriteLine("Could not display error dialog. Please check console output.");
            }
            
            // If we can't initialize the main window, we should terminate gracefully
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Console.WriteLine("Shutting down application due to initialization error.");
                desktop.Shutdown(1);
            }
            return;
        }

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