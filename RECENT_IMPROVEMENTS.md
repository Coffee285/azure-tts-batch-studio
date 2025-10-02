# Recent Improvements

## December 2024 - Voice Support & Crash Diagnostics Update

### 1. Voice Rate & Pitch Support Detection

**Problem Fixed:** Some TTS voices (like OpenAI's "Onyx Turbo Multilingual") don't support speaking rate or pitch adjustments, but the application didn't detect this and would try to apply these settings anyway.

**Solution:**
- The application now automatically detects which voices support prosody (rate/pitch) adjustments
- When you select a voice that doesn't support rate or pitch, the controls are automatically disabled
- An orange warning message appears to inform you: "Note: This voice does not support speaking rate or pitch adjustments."
- SSML generation is optimized to skip prosody tags for unsupported voices

**Affected Voices:**
The following voices are detected as not supporting prosody adjustments:
- onyx, nova, shimmer, echo, fable, alloy (OpenAI TTS models)
- Any voice containing "gpt" or "turbo" in its name

**User Impact:**
- No more confusion about why rate changes don't work on certain voices
- Better user experience with clear feedback
- Optimized SSML generation for better compatibility

### 2. Enhanced Crash Diagnostics & Logging

**Problem Fixed:** The app would sometimes crash immediately after clicking with no error message, making it impossible to diagnose issues.

**Solution:**
- Comprehensive crash logging is now enabled automatically
- All crashes and unhandled exceptions are logged to: `%TEMP%\AzureTtsBatchStudio\crash_logs\`
- Crash logs include:
  - Detailed exception information
  - Stack traces
  - Timestamp of each event
  - Inner exception details
- Global exception handlers capture:
  - Unhandled exceptions in the main application domain
  - Unobserved task exceptions (background tasks)
  - Application initialization errors

**User Impact:**
- If the app crashes, you'll see the log file location in the console
- Share the crash log files with support for faster issue resolution
- Better diagnostics for troubleshooting startup issues

**Log File Location:** 
Check `C:\Users\<YourUsername>\AppData\Local\Temp\AzureTtsBatchStudio\crash_logs\` for crash logs.

### 3. Save Profile Button - Quick Settings Save

**Problem Fixed:** Saving your TTS configuration required navigating to the Settings window through a menu.

**Solution:**
- A new "Save Profile" button is now prominently displayed in the main window header
- Green button for easy visibility
- Located between the title and the "Logs" button

**What Gets Saved:**
When you click "Save Profile", the following settings are saved as your defaults:
- Output directory
- Selected language
- Selected voice
- Speaking rate
- Pitch
- Audio format (WAV, MP3, OGG)
- Quality setting

**User Impact:**
- Quickly save your preferred configuration without opening Settings
- Your saved profile will be used as defaults the next time you open the app
- Confirmation message appears: "Profile saved successfully!"

## How to Use the New Features

### Voice Rate Support
1. Select a voice from the dropdown
2. If the voice doesn't support rate/pitch, you'll see:
   - The sliders become grayed out (disabled)
   - An orange warning message below the controls
3. For supported voices, the sliders remain active and functional

### Crash Diagnostics
1. If the app crashes, check your console output
2. Look for the line: "Log file: [path]"
3. Navigate to the log file location
4. Share the log file when reporting issues

### Save Profile Button
1. Configure your TTS settings in the main window (voice, rate, pitch, format, etc.)
2. Click the green "Save Profile" button in the top-right header
3. Wait for the confirmation message: "Profile saved successfully!"
4. Your settings are now saved as defaults

## Technical Details

### Voice Detection Logic
The application uses pattern matching to detect voices without prosody support:
- Checks voice name against known patterns (onyx, nova, shimmer, echo, fable, alloy, gpt, turbo)
- Case-insensitive matching
- SSML is generated without `<prosody>` tags for unsupported voices

### Exception Handling
Three levels of exception handling:
1. **AppDomain.UnhandledException** - Catches all unhandled exceptions
2. **TaskScheduler.UnobservedTaskException** - Catches background task exceptions
3. **Try-catch blocks** - In critical initialization code

### Settings Storage
Profile settings are stored in JSON format at:
`%APPDATA%\AzureTtsBatchStudio\appsettings.json`

## Backward Compatibility

All changes are backward compatible:
- Existing settings files continue to work
- The new `SupportsSpeakingRate` and `SupportsPitch` properties default to `true`
- Legacy voices without these properties will be treated as supporting prosody
