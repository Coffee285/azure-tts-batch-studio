using Avalonia;
using System;
using System.IO;

namespace AzureTtsBatchStudio;

sealed class Program
{
    private static string? _logFilePath;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize crash logging early
        InitializeCrashLogging();
        
        // Register global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        LogMessage("Azure TTS Batch Studio starting...");
        LogMessage($"Arguments: {string.Join(" ", args)}");
        
        try
        {
            LogMessage("Building Avalonia app...");
            var app = BuildAvaloniaApp();
            LogMessage("Starting with classic desktop lifetime...");
            app.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) when (IsDisplayException(ex))
        {
            LogMessage($"Display exception caught: {ex.Message}");
            LogException(ex);
            HandleDisplayError(ex);
        }
        catch (Exception ex)
        {
            LogMessage($"CRITICAL ERROR: An unexpected error occurred: {ex.Message}");
            LogException(ex);
            Console.WriteLine($"CRITICAL ERROR: An unexpected error occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine($"Log file: {_logFilePath}");
            Console.WriteLine("Please check the application logs for more details.");
            Console.WriteLine("Press any key to exit...");
            try
            {
                Console.ReadKey();
            }
            catch
            {
                // Ignore if console read fails
            }
            Environment.Exit(1);
        }
        
        LogMessage("Application exiting normally.");
    }

    private static void InitializeCrashLogging()
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var logDir = Path.Combine(tempPath, "AzureTtsBatchStudio", "crash_logs");
            Directory.CreateDirectory(logDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(logDir, $"crash_log_{timestamp}.txt");
            
            LogMessage($"Crash logging initialized: {_logFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not initialize crash logging: {ex.Message}");
        }
    }

    private static void LogMessage(string message)
    {
        var timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        Console.WriteLine(timestampedMessage);
        
        if (!string.IsNullOrEmpty(_logFilePath))
        {
            try
            {
                File.AppendAllText(_logFilePath, timestampedMessage + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }

    private static void LogException(Exception ex)
    {
        LogMessage($"Exception Type: {ex.GetType().FullName}");
        LogMessage($"Exception Message: {ex.Message}");
        LogMessage($"Stack Trace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            LogMessage("Inner Exception:");
            LogException(ex.InnerException);
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogMessage("=== UNHANDLED EXCEPTION ===");
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex);
            Console.WriteLine($"UNHANDLED EXCEPTION: {ex.Message}");
            Console.WriteLine($"Log file: {_logFilePath}");
        }
        else
        {
            LogMessage($"Unknown exception object: {e.ExceptionObject}");
        }
        LogMessage($"Is Terminating: {e.IsTerminating}");
    }

    private static void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        LogMessage("=== UNOBSERVED TASK EXCEPTION ===");
        LogException(e.Exception);
        Console.WriteLine($"UNOBSERVED TASK EXCEPTION: {e.Exception.Message}");
        Console.WriteLine($"Log file: {_logFilePath}");
        
        // Mark as observed to prevent app crash
        e.SetObserved();
    }

    private static bool IsDisplayException(Exception ex)
    {
        LogMessage("Checking if exception is display-related...");
        return ex.Message.Contains("XOpenDisplay failed") ||
               ex.Message.Contains("No display") ||
               ex.Message.Contains("Cannot connect to display") ||
               ex is PlatformNotSupportedException ||
               ex is InvalidOperationException;
    }

    private static void HandleDisplayError(Exception ex)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("         Azure TTS Batch Studio - Display Error");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("ERROR: Unable to initialize the graphical user interface.");
        Console.WriteLine();
        Console.WriteLine("This application requires a display server to run the GUI.");
        Console.WriteLine();
        Console.WriteLine("Possible solutions:");
        Console.WriteLine();
        Console.WriteLine("• On Windows:");
        Console.WriteLine("  - Ensure you're running on Windows Desktop (not Server Core)");
        Console.WriteLine("  - Check that graphics drivers are properly installed");
        Console.WriteLine("  - Try running as Administrator if permission issues exist");
        Console.WriteLine();
        Console.WriteLine("• On Linux:");
        Console.WriteLine("  - Install a desktop environment (GNOME, KDE, XFCE, etc.)");
        Console.WriteLine("  - Ensure X11 or Wayland display server is running");
        Console.WriteLine("  - Set DISPLAY environment variable if using SSH");
        Console.WriteLine("  - For headless servers, use X Virtual Framebuffer:");
        Console.WriteLine("    sudo apt-get install xvfb");
        Console.WriteLine("    xvfb-run -a dotnet run --project AzureTtsBatchStudio");
        Console.WriteLine();
        Console.WriteLine("• On macOS:");
        Console.WriteLine("  - Ensure you're running from Terminal.app or a GUI session");
        Console.WriteLine("  - Check that XQuartz is installed for X11 applications");
        Console.WriteLine();
        Console.WriteLine("• Remote Connections:");
        Console.WriteLine("  - Enable X11 forwarding: ssh -X username@hostname");
        Console.WriteLine("  - Use VNC or RDP for remote desktop access");
        Console.WriteLine();
        Console.WriteLine($"Technical details: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine($"Log file: {_logFilePath}");
        Console.WriteLine();
        Console.WriteLine("For more help, visit: https://github.com/Saiyan9001/azure-tts-batch-studio");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        Environment.Exit(1);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
