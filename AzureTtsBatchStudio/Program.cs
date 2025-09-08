using Avalonia;
using System;

namespace AzureTtsBatchStudio;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("Azure TTS Batch Studio starting...");
        Console.WriteLine($"Arguments: {string.Join(" ", args)}");
        
        try
        {
            Console.WriteLine("Building Avalonia app...");
            var app = BuildAvaloniaApp();
            Console.WriteLine("Starting with classic desktop lifetime...");
            app.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) when (IsDisplayException(ex))
        {
            Console.WriteLine($"Display exception caught: {ex.Message}");
            HandleDisplayError(ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL ERROR: An unexpected error occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine("Please check the application logs for more details.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
        
        Console.WriteLine("Application exiting normally.");
    }

    private static bool IsDisplayException(Exception ex)
    {
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
