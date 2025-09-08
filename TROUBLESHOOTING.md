# Troubleshooting Window Display Issues

If you're experiencing issues where the application loads (appears in Task Manager) but no window shows up, the latest fixes should resolve these issues.

## Recent Fixes Applied (Latest Version)

✅ **Fixed async initialization issues** - Removed problematic `async void` methods that could cause silent failures
✅ **Added explicit window show** - Window is now explicitly shown and activated
✅ **Improved error handling** - Better error logging and user feedback
✅ **Enhanced window visibility** - Multiple attempts to ensure window appears and is brought to front
✅ **Robust initialization** - Graceful fallbacks when services fail to load

## Quick Test (Windows)

Run the included test script:
```powershell
.\test_windows_display.ps1
```

This will test the application and show detailed output to help diagnose any remaining issues.

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
Explicitly showing main window...
Main window shown successfully.
Main window activated and brought into view.
Application framework initialization completed successfully.
```

## Common Issues and Solutions

### Issue: Application starts but no window appears ✅ FIXED
**Previous Issue**: Async initialization problems and missing explicit Show() calls
**Solution**: The latest version adds comprehensive error handling and explicit window showing with multiple fallback mechanisms.

### Issue: Window appears off-screen ✅ FIXED
**Previous Issue**: Window positioning not properly set
**Solution**: The window now explicitly starts at center screen with size 1200x800 and is activated.

### Issue: Application hangs during startup ✅ IMPROVED
**Previous Issue**: Async initialization could hang indefinitely
**Solution**: Added 30-second timeout for initialization and graceful fallbacks for service failures.

### Issue: Settings or theme loading fails ✅ FIXED
**Previous Issue**: Application would fail silently if settings couldn't be loaded
**Solution**: Application now gracefully handles settings failures and falls back to defaults without preventing window display.

### Issue: Silent initialization errors ✅ FIXED
**Previous Issue**: Errors during startup were not visible to users
**Solution**: Added comprehensive console logging and better error reporting.

## Error Messages

The application now provides clear error messages for common issues:

- **Display server issues**: Clear instructions for Windows, Linux, and macOS
- **Initialization failures**: Specific error messages with troubleshooting steps
- **Timeout errors**: Indicates if initialization is taking too long
- **Service failures**: Graceful handling of Azure TTS service configuration issues

## Windows 11 Specific Fixes

**"Windows protected your PC" warning**
- Click "More info" then "Run anyway"
- This happens with new unsigned applications
- Add the folder to Windows Defender exceptions if needed

**"App won't start" on Windows 11**
- ✅ Latest fixes ensure proper window initialization
- Try running as administrator if still having issues
- Check if .NET 8 runtime is properly installed

**"Missing DLL" errors**
- Use the self-contained portable version (includes all dependencies)
- Avoid extracting to OneDrive/cloud folders initially
- Make sure antivirus isn't blocking files

## Getting Help

If you're still experiencing issues after applying these fixes:

1. Run the test script (`test_windows_display.ps1`) and share the output
2. Or run `dotnet run` from the `AzureTtsBatchStudio` directory and share the console output
3. Include your operating system and .NET version (`dotnet --version`)
4. Check if antivirus software is interfering

The detailed logging will help identify any remaining issues. Most window display problems should now be resolved with the latest fixes.