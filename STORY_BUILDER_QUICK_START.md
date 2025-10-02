# Story Builder - Quick Start Guide

## Welcome to Story Builder!

This guide will help you create your first horror story for audio conversion in just a few minutes.

## Prerequisites

### 1. Configure Your OpenAI API Key

**IMPORTANT**: You need an OpenAI API key to use Story Builder's AI features.

1. Go to **Settings** (click the Settings button in the top right)
2. Scroll to **Story Builder Configuration**
3. Enter your OpenAI API key (starts with `sk-...`)
4. Click **Save**

**Don't have an API key yet?**
- Visit: https://platform.openai.com/api-keys
- Sign up or log in
- Click "Create new secret key"
- Copy and paste into Settings

### 2. Choose Your Story Type

We've provided templates for popular horror genres:
- **Horror** - Psychological, supernatural, atmospheric
- **Deep Web** - Techno-horror, cyber-mystery, digital nightmares
- **Alien** - Sci-fi horror, first contact, cosmic dread
- **Anachronism** - Time displacement, historical horror, time loops

Find templates in the `StoryTemplates/` folder.

## Method 1: Using Story Builder V1 (Classic)

### Step 1: Create a Project

1. Go to the **Story Builder** tab
2. Click **New**
3. Enter a project name (e.g., "Haunted Office")
4. Click OK

### Step 2: Set Up Your Story

**Instructions Panel** (Right side):
```
Write in third person, suspenseful tone.
Modern horror setting - office building.
Focus on atmosphere over gore.
Use sounds to build tension.
Target: 15 minutes (about 2,250 words).
```

**Model Parameters**:
- Model: `gpt-4` (best quality) or `gpt-3.5-turbo` (faster/cheaper)
- Temperature: `0.8` (good balance of creativity and coherence)
- Max Output Tokens: `2000`
- Context Budget: `32000`

### Step 3: Generate Your Story

**Example Prompts**:

*For Horror*:
```
Write the opening scene of a horror story about a woman working alone 
in a high-rise office building at 3 AM. She hears footsteps approaching 
her cubicle, but when the lights go out, she realizes she's not alone. 
Include sound effect markers like [SFX:footsteps_echo] and build tension 
slowly. End on a cliffhanger.
```

*For Deep Web*:
```
Write the opening of a story about a computer programmer who discovers 
a mysterious website that seems to know everything about them. Include 
realistic tech details and sound effects like [SFX:keyboard_typing] and 
[SFX:notification_ping]. Make it paranoid and unsettling.
```

*For Alien*:
```
Write the opening of a sci-fi horror story about a SETI researcher who 
receives an unexpected signal from space. It's not a greeting - it's a 
warning. Include space ambience sounds and build cosmic dread. Use 
sound effects like [SFX:radio_static] and [SFX:computer_beep].
```

*For Anachronism*:
```
Write the opening of a time displacement horror story about someone who 
finds a photograph from 1947 showing themselves in it. Include period 
details and temporal distortion effects. Use [SFX:clock_ticking] and 
[SFX:temporal_distortion].
```

### Step 4: Continue Your Story

1. Click **Send** to generate the first part
2. Wait for the AI to complete (watch the streaming output)
3. Click **Save As Part** to save it as "001.txt"
4. For the next part, write: "Continue the story. [Brief direction for next scene]"
5. Click **Send** again
6. Repeat until your story is complete

### Step 5: Export for TTS

1. Click **Export** to combine all parts
2. Switch to the **Text-to-Speech** tab
3. Paste your story
4. Select a voice (e.g., "en-US-GuyNeural" for male narrator)
5. Click **Generate Speech**

## Method 2: Using Story Builder V2 (Advanced)

*Note: V2 is more structured and includes beat-by-beat generation with templates*

### Step 1: Create Project
1. Go to Story Builder V2 (if enabled in settings)
2. Click **New**
3. Configure your story metadata (title, genre, duration)

### Step 2: Generate Outline
1. Click **Generate Outline**
2. Review the 15-20 beat structure
3. Edit beats as needed

### Step 3: Draft Beats
1. Select a beat
2. Click **Draft Beat**
3. AI generates 600-1200 words for that beat
4. Repeat for all beats

### Step 4: Refine and Convert
1. Use **Refine** to improve prose
2. Use **SSMLify** to convert to speech markup
3. Render audio with **Render Beat**

## Story Duration Guidelines

For audio conversion at 150 words per minute (WPM):

| Duration | Word Count | Suggested Beats |
|----------|-----------|-----------------|
| 15 min   | 2,250 words | 3-5 beats |
| 25 min   | 3,750 words | 6-10 beats |
| 35 min   | 5,250 words | 10-15 beats |
| 45 min   | 6,750 words | 15-20 beats |

## Tips for Great Horror Stories

### Pacing
- **First 20%**: Setup and normal world
- **Next 50%**: Building tension and escalation
- **Next 20%**: Descent into horror
- **Last 10%**: Climax and resolution

### Atmosphere
- Use all five senses (not just sight)
- Include ambient sounds with [SFX:...] markers
- Describe what characters feel, not just see
- Build slowly - don't rush to the scary parts

### Sound Effects
Always include sound effect markers in your prompts:
- `[SFX:door_creak]` - creaking door
- `[SFX:footsteps_echo]` - echoing footsteps
- `[SFX:heartbeat_fast]` - rapid heartbeat
- `[SFX:wind_howl]` - howling wind
- `[SFX:static_radio]` - radio static
- `[SFX:breathing_heavy]` - heavy breathing

### Ending Your Story
- Don't over-explain the horror
- Leave some mystery
- Consider an ambiguous or twist ending
- Optional: hint at lingering threat

## Common Mistakes to Avoid

❌ **Too Fast**: Don't rush to the horror. Build atmosphere first.
❌ **Over-explaining**: Mystery is scarier than full explanation.
❌ **Generic**: Avoid overused tropes (haunted dolls, jump scares).
❌ **No Sounds**: Remember to include [SFX:...] markers for audio.
❌ **Wrong Length**: Check word count to hit your target duration.

## Prompt Templates

### Opening Scene Prompts

**Mystery Opening**:
```
Write an opening scene that establishes normalcy before introducing 
something subtly wrong. Use [Location], [Character name], and [Time]. 
Include 2-3 sound effect markers. 800 words. End with something unsettling.
```

**Action Opening**:
```
Start in the middle of a tense moment. [Character] is already aware 
something is wrong. Build immediate tension. Use sound effects to enhance 
atmosphere. 700 words. End on a cliffhanger.
```

### Middle Scene Prompts

**Investigation**:
```
Continue the story. [Character] investigates the strange occurrences. 
Include a discovery that makes things worse. Add atmospheric sounds. 
1000 words. Raise the stakes.
```

**Escalation**:
```
Continue. Things are getting worse. [Character] tries to escape/solve/fight 
but encounters new obstacle. Increase tension. Include panic and fear. 
1200 words.
```

### Ending Prompts

**Climax**:
```
Write the climax. [Character] confronts the source of horror. High tension. 
Fast pacing. Include intense sound effects. 800 words. Build to peak moment.
```

**Resolution**:
```
Write the final scene. Show aftermath of climax. Provide resolution 
(or ambiguous ending). Final twist optional. 600 words. Leave lingering 
unease.
```

## Model Selection Guide

### GPT-4
- **Best for**: High-quality prose, complex plots
- **Cost**: Higher (~$0.03-0.06 per 1000 tokens)
- **Speed**: Slower
- **Use when**: Quality matters most

### GPT-3.5-Turbo
- **Best for**: Faster drafts, iteration
- **Cost**: Lower (~$0.0015-0.002 per 1000 tokens)
- **Speed**: Faster
- **Use when**: Drafting or on budget

### Temperature Guide
- **0.6-0.7**: More focused, consistent, good for refining
- **0.8**: Balanced creativity and coherence (recommended)
- **0.9-1.0**: More creative and unpredictable, good for ideas

## Next Steps

1. **Read the Templates**: Check out the genre templates for detailed guidance
2. **Experiment**: Try different prompts and see what works
3. **Iterate**: Generate, edit, regenerate specific parts
4. **Test Audio**: Convert small sections to test pacing
5. **Build Library**: Save successful prompts for future use

## Troubleshooting

**"API Key Invalid"**
- Check Settings → Story Builder Configuration
- Ensure key starts with "sk-"
- Verify key is active on OpenAI platform

**"Generation Too Short"**
- Increase Max Output Tokens (try 2000-3000)
- Be more specific in prompt about desired length
- Try "Write 1000 words about..."

**"Story Not Scary"**
- Check temperature (try 0.8-0.9)
- Be more specific about tone in instructions
- Add examples of desired atmosphere
- Use "unsettling", "dread", "tension" in prompts

**"Inconsistent Characters"**
- Add character details to Instructions panel
- Reference earlier parts in prompts
- Increase K value to include more context

**"Too Expensive"**
- Use GPT-3.5-Turbo instead of GPT-4
- Reduce Max Output Tokens
- Generate smaller sections
- Edit and combine manually

## Resources

- **Templates Folder**: Pre-built story templates for each genre
- **README Files**: Detailed documentation
- **Settings**: Configure API keys and preferences
- **OpenAI Docs**: https://platform.openai.com/docs

---

**Ready to create your first horror story?**

1. Set up your API key in Settings
2. Create a new project
3. Choose a template or genre
4. Write your first prompt
5. Let the AI help you craft terror!

Happy horror writing! 👻🎙️
