using Avalonia.Controls;
using System;

namespace AzureTtsBatchStudio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            Console.WriteLine("Initializing MainWindow...");
            
            // Set properties before InitializeComponent to ensure they take effect
            this.WindowState = WindowState.Normal;
            this.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen;
            this.Width = 1200;
            this.Height = 800;
            this.MinWidth = 900;
            this.MinHeight = 700;
            this.Topmost = false; // Make sure it's not hidden behind other windows
            this.Title = "Azure TTS Batch Studio";
            
            InitializeComponent();
            Console.WriteLine("MainWindow InitializeComponent completed.");
            
            Console.WriteLine($"MainWindow initialized successfully. Size: {Width}x{Height}, Location: {WindowStartupLocation}");
            
            // Ensure window is properly configured for visibility
            this.Activated += (s, e) => Console.WriteLine("MainWindow activated");
            this.Loaded += (s, e) => Console.WriteLine("MainWindow loaded");
            this.Opened += (s, e) => Console.WriteLine("MainWindow opened");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing MainWindow: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // Re-throw to be caught by the calling code
        }
    }
}