# Story Builder

The Story Builder is an AI-powered long-form story creation tool integrated into Azure TTS Batch Studio. It allows you to create engaging stories using OpenAI's language models and then convert them to speech using the existing TTS functionality.

## Features

### Project Management
- **Project-based Organization**: Each story is organized as a project with its own folder structure
- **Persistent Settings**: Each project remembers its model parameters, instructions, and preferences
- **Story Parts**: Break down long stories into manageable parts (001.txt, 002.txt, etc.)
- **Automatic Backup**: All content is automatically saved to disk as you work

### AI-Powered Generation
- **OpenAI Integration**: Uses OpenAI's latest language models for high-quality text generation
- **Streaming Output**: See your story being written in real-time
- **Context Management**: Automatically manages token budgets and includes recent story parts as context
- **Customizable Parameters**: Fine-tune temperature, top-p, max tokens, and other model parameters

### Writing Tools
- **Instructions System**: Set persistent writing style and tone guidelines for each project
- **Topic Bank**: Create weighted lists of topics for random inspiration
- **Smart Context**: Automatically includes recent story parts for continuity
- **Token Budgeting**: Intelligent management of context length with visual feedback

### Duration Planning
- **Target Length**: Set target story length in minutes of speech
- **WPM Calculation**: Configurable words-per-minute for accurate duration estimation
- **Progress Tracking**: Visual progress indicators showing completion percentage

## Getting Started

### 1. Configure OpenAI API Key
1. Go to **Settings** in the main application
2. Navigate to the **Story Builder Configuration** section
3. Enter your OpenAI API key (starts with `sk-...`)
4. Optionally set a custom projects root path
5. Click **Save**

### 2. Create Your First Project
1. Switch to the **Story Builder** tab
2. Click **New** to create a new project
3. Alternatively, click **Open** to select an existing project folder

### 3. Set Up Your Story
1. **Instructions**: Write your style guidelines and tone preferences in the Instructions panel
2. **Model Parameters**: Adjust temperature (creativity), top-p (focus), and token limits
3. **Duration Goal**: Set your target story length in minutes

### 4. Generate Content
1. **Write a Prompt**: Enter your story prompt in the Story Console
2. **Send**: Click **Send** to generate new content
3. **Continue**: Use **Continue** to extend existing content
4. **Save**: Click **Save As Part** to save generated content as a story part

## Project Structure

Each Story Builder project creates the following folder structure:

```
ProjectName/
├── instructions.txt          # Writing style and tone guidelines
├── story_parts/             # Individual story segments
│   ├── 001.txt
│   ├── 002.txt
│   └── ...
├── sessions/                # Generation metadata and logs
├── scratch/                 # Working files
│   ├── topics.json         # Topic bank for inspiration
│   └── directives.json     # Guided writing directives
├── exports/                 # Final output files
└── .project.json           # Project configuration
```

## Advanced Features

### Topic Bank
- Add topics with different weights for random selection
- Use **Pick Random Topic** for single inspiration
- Use **Pick 3** for multiple topic combinations
- **Shuffle** to randomize topic order

### Context Management
- **K Recent Parts**: Controls how many previous parts are included as context
- **Context Budget**: Total token limit including instructions, context, and output
- **Smart Truncation**: Automatically trims content to fit within token limits

### Model Parameters
- **Temperature** (0-2): Controls creativity and randomness
- **Top P** (0-1): Controls focus and coherence
- **Max Output Tokens**: Limits the length of each generation
- **Streaming**: Enable real-time text generation display

## Integration with TTS

The Story Builder is designed to work seamlessly with the existing TTS functionality:

1. **Create your story** using the Story Builder
2. **Export or copy** your completed story parts
3. **Switch to the Text-to-Speech tab**
4. **Paste your content** and convert to audio
5. **Use the same project structure** for organized audio output

## Tips for Best Results

### Writing Effective Instructions
- Be specific about style, tone, and genre preferences
- Include character and world-building guidelines
- Mention pacing and narrative structure preferences
- Example: "Write in a suspenseful, noir style with short, punchy sentences. Focus on atmospheric descriptions and character development."

### Managing Context
- Keep story parts to reasonable lengths (500-2000 words)
- Use descriptive part names for better organization
- Regularly review and edit parts for consistency
- Adjust K value based on story complexity

### Using Topics Effectively
- Create a diverse topic bank with various weights
- Include character names, locations, plot devices, and themes
- Use weighted selection to favor certain types of content
- Combine multiple topics for interesting plot combinations

## Troubleshooting

### API Issues
- **Invalid API Key**: Ensure your OpenAI API key is correct and has sufficient credits
- **Rate Limits**: If you encounter rate limiting, wait a moment before trying again
- **Network Errors**: Check your internet connection and firewall settings

### Generation Problems
- **Poor Quality**: Adjust temperature and top-p parameters
- **Inconsistency**: Increase K value to include more context
- **Token Limits**: Reduce max output tokens or increase context budget
- **Repetition**: Lower temperature or use different prompts

### Project Issues
- **Can't Save**: Check folder permissions in your projects directory
- **Missing Parts**: Verify the story_parts folder exists and is accessible
- **Settings Not Saved**: Ensure the .project.json file is writable

## Keyboard Shortcuts

- **Ctrl+Enter**: Send prompt for generation
- **Esc**: Cancel current generation
- **Ctrl+S**: Save current output as story part
- **F5**: Continue generation from current output
- **Ctrl+L**: Focus on console/log area
- **Ctrl+/**: Toggle theme (dark/light)

## Support

For issues, feature requests, or questions about the Story Builder:

1. Check the main application's troubleshooting guide
2. Verify your OpenAI API key and settings
3. Review the console log for error messages
4. Visit the project repository for updates and documentation

## Version History

- **v1.0**: Initial Story Builder implementation
  - Basic project management
  - OpenAI API integration
  - Streaming text generation
  - Context management
  - Token budgeting
  - Topic bank functionality