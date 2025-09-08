# Troubleshooting Window Display Issues

If you're experiencing issues where the application loads (appears in Task Manager) but no window shows up, please try the following:

## Quick Test (Windows)

Run the included test script:
```powershell
.\test_windows_display.ps1
```

This will test the application and show detailed output to help diagnose any issues.

## Manual Testing

Run the application from command line to see detailed error messages:
```bash
cd AzureTtsBatchStudio
dotnet run
```

You should see detailed console output like:
```
Azure TTS Batch Studio starting...
Building Avalonia app...
Starting with classic desktop lifetime...
Starting application framework initialization...
Creating main window...
MainWindow initialized successfully. Size: 1200x800, Location: CenterScreen
Application framework initialization completed successfully.
```

## Common Issues and Solutions

### Issue: Application starts but no window appears
**Solution**: The fixes in this version add comprehensive error handling and logging. Check the console output for specific error messages.

### Issue: Window appears off-screen
**Solution**: The window now explicitly starts at center screen with size 1200x800.

### Issue: Application hangs during startup
**Solution**: Added 30-second timeout for initialization. If it times out, check console for error messages.

### Issue: Settings or theme loading fails
**Solution**: Application now gracefully handles settings failures and falls back to defaults.

## Error Messages

The application now provides clear error messages for common issues:

- **Display server issues**: Clear instructions for Windows, Linux, and macOS
- **Initialization failures**: Specific error messages with troubleshooting steps
- **Timeout errors**: Indicates if initialization is taking too long

## Getting Help

If you're still experiencing issues:

1. Run the test script (`test_windows_display.ps1`) and share the output
2. Or run `dotnet run` from the `AzureTtsBatchStudio` directory and share the console output
3. Include your operating system and .NET version (`dotnet --version`)

The detailed logging will help identify the exact cause of the issue.