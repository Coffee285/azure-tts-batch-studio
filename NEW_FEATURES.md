# New Features Documentation

## Overview

Azure TTS Batch Studio has been enhanced with several new capabilities to improve usability, intelligence, and user experience:

1. **Voice Capability Awareness** - Automatic detection and display of voice-specific features
2. **AI-Powered Text Enhancement** - "Enhance for Speech" feature using OpenAI
3. **Comprehensive SSML Validation** - Robust validation before batch processing
4. **Enhanced Voice Selection UI** - Visual indicators for voice capabilities

---

## 1. Voice Capability Awareness

### What's New

The application now automatically detects and displays which features each voice supports:
- **Prosody** (rate, pitch, volume)
- **Styles** (e.g., cheerful, sad, angry, neutral)
- **Style Degree** (intensity of emotional expression)
- **Roles** (e.g., Girl, Boy, YoungAdultFemale, narrator)

### How It Works

When you select a voice, the application:
1. Fetches capability metadata from an internal cache
2. Determines which SSML features the voice supports
3. Displays this information in the UI
4. Automatically disables unsupported controls

### Voice Capabilities Display

Below the voice settings, you'll see a line like:
```
Voice Capabilities: ✓ Prosody, ✓ Styles (20), ✓ Roles (8)
```

This tells you:
- **✓ Prosody** - Voice supports rate, pitch, and volume adjustments
- **✓ Styles (20)** - Voice supports 20 different speaking styles
- **✓ Roles (8)** - Voice supports 8 different character roles

### Known Voice Capabilities

#### Voices with Full Style Support
- **English (US):** Aria, Guy, Dave, Jenny, Jane, Jason, Sara, Tony
- **English (UK):** Sonia, Ryan
- **English (Australia):** Natasha, William
- **Chinese:** Xiaoxiao, Yunxi, Yunxia, Yunyang, and others
- **Japanese:** Nanami, Keita
- **Spanish:** Elvira, Dalia
- **French:** Denise, Henri
- **German:** Katja, Conrad
- **Italian:** Elsa, Isabella, Diego
- **Portuguese:** Francisca, Antonio

#### Voices with Role Support
- **English (US):** Aria, Dave, Guy, Jenny
- **Chinese:** Xiaoxiao, Yunxi, Yunxia
- **Japanese:** Nanami

#### Voices Without Prosody Support
- **OpenAI TTS Voices:** alloy, echo, fable, onyx, nova, shimmer
- These voices accessed through Azure OpenAI don't support rate/pitch adjustments

---

## 2. AI-Powered "Enhance for Speech" Feature

### What Is It?

The "✨ Enhance for Speech" button uses OpenAI's GPT models to transform plain text into speech-optimized SSML markup, making your audio output more natural and engaging.

### How to Use

1. **Configure OpenAI API Key**
   - Go to **Settings** → **Story Builder Configuration**
   - Enter your OpenAI API key (starts with `sk-...`)
   - Click **Save**

2. **Prepare Your Text**
   - Enter or paste your plain text in the main text area
   - Select your desired voice

3. **Enhance**
   - Click the **✨ Enhance for Speech** button
   - Wait for AI to process (usually 5-15 seconds)
   - Your text will be replaced with SSML-enhanced version

4. **Generate Speech**
   - Click **Generate Speech** as usual
   - The enhanced SSML will produce more natural audio

### What Gets Enhanced?

The AI enhancement adds:
- **Natural Pauses** - `<break time="500ms"/>` for breathing room
- **Emphasis** - `<emphasis>` tags for important words
- **Prosody Adjustments** - Rate, pitch, and volume changes for emotion
- **Style Expressions** - Emotional tones like cheerful, sad, excited
- **Better Flow** - Improved sentence structure for speech

### Example

**Before Enhancement:**
```
Hello. Welcome to our product demo. Today we'll show you amazing features.
```

**After Enhancement:**
```xml
<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis' 
       xmlns:mstts='http://www.w3.org/2001/mstts'>
    <voice name='en-US-AriaNeural'>
        <mstts:express-as style='cheerful'>
            <prosody rate='medium' pitch='+5%'>
                Hello! <break time='300ms'//>
                Welcome to our <emphasis level='strong'>product demo</emphasis>.
                <break time='500ms'/>
                Today, we'll show you <emphasis>amazing</emphasis> features!
            </prosody>
        </mstts:express-as>
    </voice>
</speak>
```

### Smart Voice-Aware Enhancement

The enhancement is intelligent:
- If the voice doesn't support prosody, AI won't add rate/pitch tags
- If the voice doesn't support styles, AI won't add style expressions
- Only valid, supported SSML features are used

### Requirements

- **OpenAI API Key** - From https://platform.openai.com/api-keys
- **Internet Connection** - To connect to OpenAI API
- **Selected Voice** - Must choose a voice before enhancing
- **Non-Empty Text** - Must have text to enhance

### Troubleshooting

**"OpenAI API key not configured"**
- Go to Settings and enter your API key

**"Enhancement failed"**
- Check your internet connection
- Verify your API key is valid and has credits
- Try with shorter text (limit is ~1000 words)

**"Rate limit exceeded"**
- Wait a few seconds and try again
- OpenAI has rate limits based on your plan

---

## 3. SSML & Batch Validation

### Automatic Validation

Before processing any batch, the application now validates:
- ✅ **Input text** is not empty
- ✅ **Voice** is selected
- ✅ **Output directory** exists
- ✅ **SSML** is well-formed (if using SSML)
- ✅ **Voice compatibility** with SSML features

### Validation Messages

You'll see clear error messages if validation fails:

**Error Examples:**
- ❌ "Input text is required"
- ❌ "Voice selection is required"
- ❌ "Output directory does not exist: /invalid/path"
- ❌ "SSML is not well-formed XML: Missing closing tag"

**Warning Examples:**
- ⚠️ "Voice 'Alloy' does not support rate adjustments. The rate attribute will be ignored."
- ⚠️ "Style 'excited' may not be supported by voice 'Guy'. Available styles: cheerful, sad, angry..."
- ⚠️ "Invalid break time '5000ms'. Break time should end with 'ms' or 's'"

### SSML Tag Validation

The validator checks:
- **Supported Tags** - Only Azure TTS supported tags
- **Prosody Values** - Valid rate and pitch values
- **Break Times** - Proper time formats (e.g., "500ms", "2s")
- **Style Names** - Valid style names for selected voice
- **Role Names** - Valid role names for selected voice

### Validation Results

After validation, you'll see a summary:
```
✅ Validation passed successfully
```

Or:
```
❌ 2 Error(s):
  • Input text is required
  • Output directory does not exist
  
⚠️ 1 Warning(s):
  • Voice does not support rate adjustments
```

---

## 4. Enhanced Preview Mode

### What's Improved

The preview mode now includes:
- Better error handling
- Actual Azure TTS call with current settings
- Temporary file cleanup
- Status messages for debugging

### How to Use

1. Select a voice
2. Adjust settings (rate, pitch, etc.)
3. Click **Preview Selected Voice**
4. Listen to the sample audio
5. Adjust and preview again until satisfied

### Preview Text

The default preview text is:
> "Hello! This is a preview of the selected voice."

The preview uses:
- Selected voice
- Current speaking rate
- Current pitch
- WAV format for best quality

---

## 5. OpenAI API Key Management

### Secure Storage

- **Session-Only** - API key is stored in memory only during app session
- **Not Persisted** - Key is saved to encrypted settings file (not plain text)
- **Protected Input** - Password-masked field in UI

### Configuration

1. Open **Settings** (gear icon or File → Settings)
2. Scroll to **Story Builder Configuration**
3. Enter your OpenAI API key in the masked field
4. Click **Save**
5. Key is now available for enhancement feature

### Getting an API Key

1. Go to https://platform.openai.com/api-keys
2. Sign up or log in
3. Click "Create new secret key"
4. Copy the key (starts with `sk-...`)
5. Paste into Azure TTS Batch Studio settings

### Pricing

OpenAI charges per token:
- **GPT-4 Turbo** - ~$0.01 per 1000 input tokens
- **GPT-3.5 Turbo** - ~$0.0015 per 1000 input tokens

Typical enhancement costs:
- Short paragraph (100 words) - ~$0.001
- Full article (1000 words) - ~$0.01
- Long story (5000 words) - ~$0.05

---

## Technical Details

### New Services

#### VoiceCapabilityService
- Fetches and caches voice metadata
- Determines style/role support
- Enriches VoiceInfo objects with capabilities

#### SsmlEnhancementService
- Connects to OpenAI API
- Generates voice-aware SSML
- Validates enhanced output

#### SsmlValidationService
- Validates XML structure
- Checks voice compatibility
- Provides actionable error messages

### Architecture

```
MainWindowViewModel
  ├─ VoiceCapabilityService (caching)
  ├─ SsmlEnhancementService (OpenAI)
  └─ SsmlValidationService (validation)
```

### Performance

- **Voice Capabilities** - Cached, instant lookup
- **SSML Enhancement** - 5-15 seconds (depends on OpenAI API)
- **Validation** - < 100ms for typical SSML

---

## Backward Compatibility

✅ **100% Compatible**
- Existing projects work unchanged
- No breaking changes to existing features
- New features are optional
- Graceful fallback if OpenAI unavailable

---

## Future Enhancements

Possible future improvements:
- Real-time SSML validation as you type
- SSML syntax highlighting
- Voice sample library
- Batch enhancement for multiple texts
- Custom AI prompts for enhancement
- Voice capability filtering in UI

---

## Support

### Issues

Report issues on GitHub: https://github.com/Coffee285/azure-tts-batch-studio/issues

### Documentation

- Main README: README.md
- Story Builder: STORY_BUILDER_README.md
- Troubleshooting: TROUBLESHOOTING.md

### Community

- GitHub Discussions
- Issue Tracker
- Pull Requests Welcome

---

## Version History

### v2.0 (Current)
- ✅ Voice capability awareness
- ✅ AI-powered text enhancement
- ✅ Comprehensive SSML validation
- ✅ Enhanced voice selection UI
- ✅ OpenAI API integration
- ✅ 97 passing unit tests

### v1.0
- Basic TTS functionality
- Batch processing
- Story Builder
- Multiple voices and languages

---

**Made with ❤️ for better speech synthesis**
