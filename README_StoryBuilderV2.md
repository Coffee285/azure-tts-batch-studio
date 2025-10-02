# Story Builder V2 - User Guide

## Overview

Story Builder V2 is a powerful AI-assisted feature for creating long-form horror stories (15-45 minutes) with Azure Text-to-Speech integration. It provides an end-to-end workflow from story planning to audio production.

## Features

### ✨ Key Capabilities

- **AI-Powered Story Generation**: Use OpenAI or Azure OpenAI to generate story outlines and beat-by-beat content
- **Template-Based Prompts**: Customizable templates for outline, beat drafting, refinement, and SSML conversion
- **Multi-Voice TTS**: Character-specific voice mapping with Azure Neural voices
- **SSML Enhancement**: Automatic conversion from Markdown to speech-optimized SSML
- **Sound Effects Integration**: Inline SFX markers that convert to audio tags
- **Cost Tracking**: Real-time token usage and cost estimation
- **Project Management**: Atomic file saves with rolling backups (keeps last 10 versions)

### 🎯 Target Use Case

Create atmospheric horror stories with:
- 15-45 minute target duration
- 10-20 narrative beats
- Multiple characters with distinct voices
- Background music and sound effects
- Professional-quality audio output

## Getting Started

### Prerequisites

1. **Azure Speech Services**
   - Azure subscription with Speech Services enabled
   - Subscription key and region

2. **LLM Provider** (choose one)
   - OpenAI API key (for OpenAI)
   - OR Azure OpenAI endpoint + API key + deployment name

### Initial Setup

1. **Enable Story Builder V2**
   - Open Settings
   - Set `StoryBuilderV2Enabled` to `true`

2. **Configure LLM Provider**
   
   **For OpenAI:**
   ```json
   {
     "LlmProvider": "OpenAI",
     "LlmBaseUrl": "https://api.openai.com/v1",
     "LlmApiKey": "sk-...",
     "LlmModel": "gpt-4"
   }
   ```

   **For Azure OpenAI:**
   ```json
   {
     "LlmProvider": "AzureOpenAI",
     "LlmBaseUrl": "https://your-instance.openai.azure.com",
     "LlmApiKey": "your-api-key",
     "AzureOpenAIDeployment": "your-deployment-name"
   }
   ```

3. **Configure Azure TTS**
   - Set `AzureSubscriptionKey`
   - Set `AzureRegion` (e.g., "eastus")

4. **Test Connections**
   - Click "Test Connection" buttons to verify both LLM and TTS services

## Project Structure

Each project is stored in its own folder with the following structure:

```
ProjectName/
├── project.json          # Main project file
├── .backups/            # Rolling backups (last 10 saves)
├── beats/               # Individual beat markdown files
├── audio/               # Generated audio files
├── exports/             # Final exported files
└── sfx/                 # Sound effects library
```

## Workflow

### 1. Create a Project

Click **New** to create a new project. The system will:
- Generate a unique project folder
- Initialize project metadata
- Create directory structure

### 2. Generate Outline

Click **Generate Outline** to create a story structure:
- 10-20 beats with titles and summaries
- Pacing curve (slow burn → climax → resolution)
- SFX suggestions per beat
- Estimated word counts

**Template Variables:**
- `${title}` - Story title
- `${genre}` - Genre (Horror)
- `${duration.MinMinutes}` - Minimum duration
- `${style.NarrativeVoice}` - Narrative perspective
- `${style.ToneAnchors}` - Tone guidelines

### 3. Draft Beats

Select a beat and click **Draft Beat** to generate full prose:
- 600-1200 words per beat
- Atmospheric, tight prose
- Inline SFX markers: `[SFX:door_creak]`
- Micro-cliffhanger endings

**Output Format:**
```markdown
## Beat Title

The door creaked open [SFX:door_creak] as Sarah stepped into 
the darkness. Her flashlight flickered once, then died.
```

### 4. Refine Content

Use **Refine** to improve a beat:
- Remove clichés
- Enhance atmosphere
- Improve pacing
- Maintain continuity

### 5. Convert to SSML

The SSML builder automatically converts Markdown to SSML:
- Character voice mapping
- Prosody adjustments (rate, pitch, volume)
- Break tags for pauses
- Audio tags for SFX

**Example Output:**
```xml
<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis">
  <voice name="en-US-AvaNeural">
    <prosody rate="1.0" pitch="+0%" volume="+0dB">
      The door creaked open <audio src="sfx/door_creak.wav" /> as Sarah...
    </prosody>
  </voice>
</speak>
```

### 6. Render Audio

Click **Render Beat** to generate audio:
- Azure TTS synthesis
- Background music integration (with ducking)
- SFX placement
- Export as WAV/MP3

## Templates

Templates are stored in `%AppData%\AzureTtsBatchStudio\templates\` and use simple variable substitution.

### Editing Templates

1. Navigate to templates directory
2. Copy default template (e.g., `Outline.tmpl`)
3. Edit with your preferred text editor
4. Save with same filename
5. User template overrides default

### Template Syntax

Variables use `${variable}` or `${object.property}` syntax:

```
Title: ${title}
Duration: ${duration.MinMinutes}-${duration.MaxMinutes} minutes
Narrative: ${style.NarrativeVoice}
Characters: ${characters}
```

### Available Templates

1. **Outline.tmpl** - Story structure generation
2. **BeatDraft.tmpl** - Beat expansion to prose
3. **Refine.tmpl** - Content refinement
4. **SSMLify.tmpl** - Markdown to SSML conversion

### Resetting Templates

To reset a template to default:
1. Delete the user template file
2. Restart the application
3. Default template will be used

## Cost Management

### Token Tracking

- **Prompt Tokens**: Input to LLM
- **Completion Tokens**: Output from LLM
- **Total Tokens**: Sum of both

### Cost Estimation

Approximate costs per 1K tokens (USD):

| Model | Prompt | Completion |
|-------|--------|------------|
| gpt-4-turbo | $0.01 | $0.03 |
| gpt-4 | $0.03 | $0.06 |
| gpt-3.5-turbo | $0.0015 | $0.002 |

### Budget Control

Set `MaxCostPerProject` in settings to prevent runaway costs. Generation will stop if exceeded.

## Content Safety

### Built-in Guardrails

Story Builder V2 includes content moderation:

1. **Automatic Checks**: Text is checked before TTS rendering
2. **Flagged Categories**: Hate speech, violence, sexual content, self-harm
3. **User Action**: Edit and retry if content is flagged

### Style Guide Constraints

Define boundaries in `StoryStyleGuide`:
- `ContentConstraints`: "No graphic violence, no sexual content"
- `TropesToAvoid`: "Jump scares, torture porn"
- `ToneAnchors`: "Unsettling, psychological, atmospheric"

## Troubleshooting

### LLM Connection Fails

**Symptoms**: "Connection failed" message

**Solutions**:
1. Verify API key is correct
2. Check base URL (includes `/v1` for OpenAI)
3. For Azure OpenAI, verify deployment name
4. Test with curl:
   ```bash
   curl https://api.openai.com/v1/models \
     -H "Authorization: Bearer YOUR_KEY"
   ```

### TTS Rendering Fails

**Symptoms**: Error during audio generation

**Solutions**:
1. Verify Azure Speech key and region
2. Check SSML validity (use Validate button)
3. Ensure voice names are correct (e.g., "en-US-AvaNeural")
4. Check Azure Speech service quota

### Generation Produces Poor Results

**Solutions**:
1. Adjust temperature (lower = more focused, higher = more creative)
2. Increase max tokens if output is truncated
3. Edit templates for better prompts
4. Refine style guide and constraints

### Project Won't Load

**Solutions**:
1. Check project.json exists
2. Verify JSON syntax (use JSONLint)
3. Restore from backup (.backups folder)
4. Create new project and copy content manually

## Best Practices

### Story Planning

1. **Start with outline** - Don't jump into beats
2. **Define characters early** - Include in project before drafting
3. **Set tone guidelines** - Use specific, evocative language
4. **Plan SFX** - Mark key moments in outline

### Content Generation

1. **Iterate** - Generate → Refine → Generate again
2. **Use examples** - Add excerpts to style guide
3. **Maintain continuity** - Reference earlier beats in prompts
4. **Lock beats** - Mark complete beats as "Locked"

### Audio Production

1. **Test first beat** - Verify voices and settings before batch rendering
2. **Use neural voices** - Higher quality for storytelling
3. **Balance SFX** - Don't overwhelm dialogue
4. **Duck music** - Enable speech ducking for clarity

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Ctrl+N | New Project |
| Ctrl+O | Open Project |
| Ctrl+S | Save Project |
| Ctrl+G | Generate Outline |
| Ctrl+D | Draft Beat |
| Ctrl+R | Refine Beat |
| Esc | Cancel Generation |

## API Limits

### OpenAI

- Rate limit: 3,500 tokens/min (gpt-4)
- Max tokens/request: 8,192 (gpt-4)
- Timeout: 120 seconds

### Azure OpenAI

- Rate limit: Deployment-specific
- Max tokens: Model-specific
- Timeout: 120 seconds

### Azure Speech

- Concurrent requests: 20
- Max SSML length: 8,000 characters
- Rate limit: Region-specific

## Support

For issues or questions:

1. Check this guide
2. Review logs: `%AppData%\AzureTtsBatchStudio\logs\`
3. Use "Copy diagnostics" button
4. Open GitHub issue: https://github.com/Coffee285/azure-tts-batch-studio

## Version History

### v1.0.0 (Initial Release)

- OpenAI and Azure OpenAI support
- Template-based story generation
- SSML builder with SFX
- Project management with backups
- Cost tracking
- Comprehensive unit tests (113 tests)

---

**Happy storytelling! 🎙️**
