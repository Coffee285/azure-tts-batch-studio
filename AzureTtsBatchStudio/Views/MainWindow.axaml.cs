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
            InitializeComponent();
            Console.WriteLine("MainWindow InitializeComponent completed.");
            
            // Ensure window is visible and has proper state
            this.WindowState = WindowState.Normal;
            this.Topmost = false; // Make sure it's not hidden behind other windows
            
            Console.WriteLine($"MainWindow initialized successfully. Size: {Width}x{Height}, Location: {WindowStartupLocation}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing MainWindow: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // Re-throw to be caught by the calling code
        }
    }
}