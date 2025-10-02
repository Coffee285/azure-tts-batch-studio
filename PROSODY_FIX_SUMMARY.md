# Voice Prosody Support Fix - Summary

## Problem Statement
The application was incorrectly disabling prosody controls (Speaking Rate and Pitch sliders) for Azure Neural voices that contain certain keywords in their names (like "turbo", "onyx", etc.), even though these voices DO support prosody adjustments.

## Root Cause
The previous implementation used broad pattern matching to detect voices without prosody support:
```csharp
var unsupportedPatterns = new[] { "onyx", "nova", "shimmer", "echo", "fable", "alloy", "gpt", "turbo" };
return unsupportedPatterns.Any(pattern => lowerVoiceName.Contains(pattern));
```

This approach incorrectly flagged Azure Neural voices like "Onyx Turbo Multilingual (Male)" as not supporting prosody, when they actually do.

## Solution Implemented

### 1. Precise Voice Detection
Updated `IsVoiceWithoutProsodySupport()` to use exact matching for OpenAI TTS voices only:
- Only marks voices as unsupported if they are exact matches to OpenAI voice names: "alloy", "echo", "fable", "onyx", "nova", "shimmer"
- Or if they start with "gpt-" or "gpt4-" (e.g., "gpt-4-turbo")
- Or if they explicitly contain "openai" in the name

### 2. Azure Neural Voice Support
All Azure Neural voices now correctly support prosody, including:
- Voices with "Turbo" in their display name (e.g., "Onyx Turbo Multilingual")
- Voices with "Multilingual" in their name
- All standard Neural voices (e.g., "en-US-AriaNeural", "en-US-GuyNeural")

### 3. SSML Generation
The SSML generation uses Azure-supported prosody values:
- **Rate**: Predefined values ("x-slow", "slow", "medium", "fast", "x-fast") based on the numeric rate slider
- **Pitch**: Percentage-based values (e.g., "+10%", "-5%", "default")

Both of these formats are fully supported by Azure Neural TTS voices.

## Testing
- Added 3 new test cases to verify Azure voices with "turbo" and similar keywords support prosody
- All 71 tests pass successfully
- Build completes without warnings

## Impact
Users can now adjust Speaking Rate and Pitch for all Azure Neural voices, providing full control over speech synthesis parameters. Only actual OpenAI TTS voices (accessed through Azure OpenAI integration) will have these controls disabled, as they genuinely don't support SSML prosody tags.

## Additional Fixes
1. **Updated System.Text.Json**: Version 8.0.0 → 9.0.9 to fix high severity vulnerabilities
2. **Simplified Build Targets**: Removed x86 and ARM64 builds, keeping only x64 as requested
3. **Updated Build Scripts**: Removed portable package generation for removed architectures
