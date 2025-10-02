# UI Changes Summary

## Visual Changes to the Application

This document describes the visual changes made to the Azure TTS Batch Studio user interface.

### 1. Main Window Header - Save Profile Button

**Location:** Top-right section of the main window, in the header area

**Before:**
```
[Azure TTS Batch Studio]                    [Logs] [Settings]
```

**After:**
```
[Azure TTS Batch Studio]      [Save Profile] [Logs] [Settings]
```

**Button Details:**
- **Text:** "Save Profile"
- **Color:** Green background with white text (success color)
- **Position:** Between the title and the Logs button
- **Behavior:** Saves current TTS configuration as default settings

**Visual Hierarchy:**
1. Title on the left
2. Save Profile (green, prominent)
3. Logs (outlined)
4. Settings (outlined)

### 2. Voice Settings Panel - Rate Control

**Location:** Right panel, "Voice Settings" card

**When Voice Supports Rate/Pitch (e.g., "Aria Neural"):**
```
Voice Settings
━━━━━━━━━━━━━━━━━━━━

Language:       [English (United States) ▼]
Voice:          [Aria (Female) ▼]

Speaking Rate:  
[━━━━●━━━━━━━] 1.00x   ← Active, movable slider

Pitch:
[━━━━●━━━━━━━] 0%      ← Active, movable slider
```

**When Voice Does NOT Support Rate/Pitch (e.g., "Onyx"):**
```
Voice Settings
━━━━━━━━━━━━━━━━━━━━

Language:       [English (United States) ▼]
Voice:          [Onyx Turbo Multilingual (Male) ▼]

Speaking Rate:  
[━━━━●━━━━━━━] 1.00x   ← Grayed out, disabled

Pitch:
[━━━━●━━━━━━━] 0%      ← Grayed out, disabled

⚠️ Note: This voice does not support speaking rate or pitch adjustments.
```

**Warning Message Details:**
- **Color:** Orange text
- **Icon:** Warning symbol (⚠️)
- **Position:** Below the disabled sliders
- **Text:** "Note: This voice does not support speaking rate or pitch adjustments."
- **Text Wrap:** Enabled for readability

### 3. Status Messages

**Save Profile Success:**
```
Status Bar:  Profile saved successfully!  [Processed: 0] [Total: 0]
```
- Appears immediately after clicking Save Profile
- Displayed for 3 seconds
- Then resets to "Ready"

**Profile Save Error:**
```
Status Bar:  Error saving profile: [error message]  [Processed: 0] [Total: 0]
```
- Shows if save fails
- Remains visible until next action

## Color Scheme

### Button Colors:
- **Save Profile Button:**
  - Background: Success Green (same as "Generate Speech")
  - Text: White
  - Border: Success Green
  - Hover: Slightly darker green

- **Logs Button:**
  - Background: Transparent
  - Text: Accent Blue
  - Border: Accent Blue
  - Hover: Light blue highlight

- **Settings Button:**
  - Background: Transparent
  - Text: Accent Blue
  - Border: Accent Blue
  - Hover: Light blue highlight

### Warning Colors:
- **Prosody Warning:**
  - Text: Orange (#FFA500 or similar)
  - Background: None (transparent)
  - Icon: Orange warning triangle

## Responsive Behavior

### Rate/Pitch Controls:
1. When enabled:
   - Slider thumb can be dragged
   - Click on track moves thumb
   - Value updates in real-time
   - Opacity: 100%

2. When disabled:
   - Slider thumb cannot be moved
   - Click has no effect
   - Value remains at current setting
   - Opacity: 50% (grayed out)
   - Cursor: not-allowed or default

### Save Profile Button:
- Always enabled (not tied to processing state)
- Shows immediate feedback on click
- Updates status message
- Can be clicked anytime

## User Workflow Examples

### Workflow 1: Using a Voice Without Rate Support
1. User selects "Onyx Turbo Multilingual (Male)" from voice dropdown
2. Rate and Pitch sliders immediately gray out
3. Warning message appears below: "⚠️ Note: This voice does not support..."
4. User sees the message and understands why controls are disabled
5. User can still adjust other settings (format, quality, etc.)
6. Clicks "Generate Speech" - works normally, just without rate/pitch

### Workflow 2: Saving Current Configuration
1. User configures voice, rate, pitch, format, quality
2. Clicks green "Save Profile" button in header
3. Status bar shows "Profile saved successfully!"
4. Message disappears after 3 seconds
5. Next time app opens, these settings are the defaults

### Workflow 3: Switching Between Voices
1. User has "Aria Neural" selected (supports rate/pitch)
2. Rate slider is at 1.5x, Pitch at +10%
3. User switches to "Onyx"
4. Sliders disable, warning appears
5. Rate and pitch values remain at 1.5x and +10% (but grayed out)
6. User switches back to "Aria Neural"
7. Sliders re-enable, values still at 1.5x and +10%
8. User can continue adjusting

## Accessibility Considerations

1. **Disabled Controls:**
   - Visual indicator: grayed out appearance
   - Cursor change: indicates non-interactivity
   - Warning text: explains why disabled

2. **Color Contrast:**
   - Warning text (orange) on light background: high contrast
   - Button text (white) on green background: high contrast
   - Disabled controls: 50% opacity maintains readability

3. **Clear Messaging:**
   - Warning text is explicit and informative
   - Button labels are action-oriented ("Save Profile")
   - Status messages provide confirmation

## Implementation Notes

### XAML Bindings Used:
```xml
<!-- Rate Slider -->
IsEnabled="{Binding IsSpeakingRateEnabled}"

<!-- Pitch Slider -->
IsEnabled="{Binding IsPitchEnabled}"

<!-- Warning Message -->
IsVisible="{Binding ProsodyWarningMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
Text="{Binding ProsodyWarningMessage}"

<!-- Save Profile Button -->
Command="{Binding SaveProfileCommand}"
```

### ViewModel Properties:
```csharp
// Controls enable/disable state
bool IsSpeakingRateEnabled
bool IsPitchEnabled

// Warning message text
string ProsodyWarningMessage

// Command for button
ICommand SaveProfileCommand
```

## Browser/Platform Compatibility

The application uses Avalonia UI framework which renders consistently across:
- Windows 10/11
- macOS
- Linux (with appropriate display server)

All visual elements (buttons, sliders, warnings) use Avalonia's built-in controls and styling system, ensuring consistent appearance across platforms.
