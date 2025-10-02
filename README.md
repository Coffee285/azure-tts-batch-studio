# Azure TTS Batch Studio

A comprehensive desktop application for batch conversion of text to speech using Azure Cognitive Services. Built with C# and Avalonia UI for cross-platform compatibility with a focus on Windows 11.

![Azure TTS Batch Studio](https://img.shields.io/badge/Platform-Windows%2011-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Build Status](https://github.com/Saiyan9001/azure-tts-batch-studio/workflows/Build%20and%20Release/badge.svg)

## Features

### 🎵 Text-to-Speech Conversion
- **Batch Processing**: Convert multiple text blocks to speech files simultaneously
- **Azure Integration**: Uses Azure Cognitive Services Speech SDK for high-quality synthesis
- **Multiple Languages**: Support for 75+ languages and locales
- **Voice Selection**: Choose from hundreds of neural and standard voices
- **Custom Parameters**: Adjust speaking rate, pitch, and voice characteristics

### 🎛️ Advanced Controls
- **SSML Support**: Full Speech Synthesis Markup Language support for fine-tuned control
- **Audio Formats**: Export to WAV, MP3, and OGG formats
- **Quality Options**: Standard, High, and Premium quality settings
- **Batch Import**: Load text from files or CSV for bulk processing

### 🖥️ Modern User Interface
- **Avalonia UI**: Cross-platform, modern desktop interface
- **Responsive Design**: Optimized for various screen sizes
- **Real-time Preview**: Test voices before batch processing
- **Progress Tracking**: Visual progress indicators for batch operations
- **Dark/Light Theme**: Automatic theme detection

### 📚 Story Builder (NEW!)
- **AI-Powered Writing**: Create long-form stories (15-45 minutes) using OpenAI GPT models
- **Genre Templates**: Pre-built templates for Horror, Deep Web, Alien, and Anachronism stories
- **Quick Start Guide**: Built-in guidance with examples and story prompts
- **Sound Effects**: Automatic [SFX:...] marker support for immersive audio
- **Project Management**: Organize stories in beats/parts with automatic saving
- **Example Projects**: Complete story examples with prompts and techniques
- **TTS Integration**: Seamlessly convert generated stories to speech

**New to Story Builder?**
- Check out the **📖 Quick Start** button in the Story Builder tab
- Explore templates in the `StoryTemplates/` folder
- See example projects in `ExampleProjects/` folder
- Read `STORY_BUILDER_QUICK_START.md` for detailed instructions

### 📦 Multiple Distribution Methods
- **MSIX Package**: Modern Windows app store-style installation
- **Traditional Installer**: Inno Setup-based installer for Windows
- **Portable Build**: No-installation-required executable
- **Self-Contained**: Includes .NET runtime, no additional dependencies

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10 version 1809 or later
- **Memory**: 4 GB RAM
- **Storage**: 500 MB available space
- **Network**: Internet connection for Azure services

### Recommended Requirements
- **Operating System**: Windows 11
- **Memory**: 8 GB RAM or more
- **Storage**: 1 GB available space
- **Processor**: Multi-core processor for better batch processing performance

### Azure Requirements
- **Azure Subscription**: Active Azure account
- **Speech Services**: Azure Cognitive Services Speech resource
- **API Key**: Valid subscription key and region

## Installation

### ⚡ Super Easy Installation for Windows 11 (Recommended)

**Option 1: Portable Version (No Installation Required)**
1. Download: `AzureTtsBatchStudio-x64-portable.zip` from [Releases](https://github.com/Saiyan9001/azure-tts-batch-studio/releases) (51MB)
2. Extract to any folder (Desktop, Documents, etc.)
3. Double-click `AzureTtsBatchStudio.exe` to run
4. That's it! No installation, no admin rights needed.

**Option 2: Windows Installer (Traditional)**
1. Download: `Azure-TTS-Batch-Studio-Setup-x64.exe` from [Releases](https://github.com/Saiyan9001/azure-tts-batch-studio/releases)
2. Double-click the installer and follow the wizard
3. Launch from Start Menu or Desktop shortcut

### Other Installation Options

**Option 3: MSIX Package**
1. Download the MSIX package from [Releases](https://github.com/Saiyan9001/azure-tts-batch-studio/releases)
2. Double-click to install (requires Windows 10/11)
3. Launch from Start Menu

**Option 4: Build from Source**
```bash
# Clone and build
git clone https://github.com/Saiyan9001/azure-tts-batch-studio.git
cd azure-tts-batch-studio
build.bat  # On Windows
# or
./build.sh  # On Linux/macOS
```

## Configuration

### Azure Speech Services Setup
1. Create an Azure account at [portal.azure.com](https://portal.azure.com)
2. Create a new "Speech Services" resource
3. Note your **Subscription Key** and **Region**
4. In the application, go to **Settings**
5. Enter your subscription key and region
6. Test the connection

### Story Builder Setup (Optional)
If you want to use the AI-powered Story Builder feature:
1. Get an OpenAI API key from [platform.openai.com/api-keys](https://platform.openai.com/api-keys)
2. In the application, go to **Settings**
3. Scroll to **Story Builder Configuration**
4. Enter your OpenAI API key (starts with `sk-...`)
5. Optionally set a custom projects root path
6. Click **Save**

**Note**: Story Builder requires an OpenAI API key and will incur API costs based on usage. GPT-4 is recommended for best quality, GPT-3.5-turbo for faster/cheaper generation.

### First Run
1. Launch Azure TTS Batch Studio
2. Click **Settings** to configure Azure credentials
3. Select your preferred default language and voice
4. Choose an output directory for generated audio files
5. Start converting text to speech!

## Usage

### Basic Text Conversion
1. Enter or paste text in the main text area
2. Select language and voice from the dropdowns
3. Adjust speaking rate and pitch if desired
4. Choose audio format and quality
5. Click **Generate Speech** to create audio files

### Batch Processing
1. Use **Load Text File** to import large text documents
2. Text is automatically split into manageable chunks
3. Each chunk becomes a separate audio file
4. Monitor progress with the built-in progress bar

### CSV Import
1. Click **Import from CSV** to load structured data
2. Each row becomes a separate audio file
3. Useful for creating multiple variations or different content

### Voice Preview
1. Select any voice from the dropdown
2. Click **Preview Selected Voice** to hear a sample
3. Adjust parameters and preview again until satisfied

### Story Builder Usage (NEW!)

**Quick Start**:
1. Go to the **Story Builder** tab
2. Click the **📖 Quick Start** button for immediate guidance
3. Click **⚙️ API Setup** to configure your OpenAI API key if not done yet

**Creating a Horror Story** (15-45 minutes):
1. Click **New** to create a new project
2. Review templates in `StoryTemplates/HorrorTemplate.md` for guidance
3. In the **Instructions** panel (right side), enter your style guidelines:
   ```
   Write in third person, suspenseful tone.
   Modern horror setting.
   Focus on atmosphere over gore.
   Use sounds to build tension.
   Target: 15 minutes (about 2,250 words).
   ```
4. Set **Model Parameters**:
   - Model: `gpt-4` (best quality) or `gpt-3.5-turbo` (faster/cheaper)
   - Temperature: `0.8` (balanced creativity)
   - Max Tokens: `2000`

5. In the **Story Console**, enter your first prompt:
   ```
   Write the opening scene of a horror story about a woman working alone 
   in a high-rise office building at 3 AM. She hears footsteps approaching, 
   but when the lights go out, she realizes she's not alone. Include sound 
   effect markers like [SFX:footsteps_echo] and build tension slowly. 
   End on a cliffhanger. 800 words.
   ```

6. Click **Send** and wait for AI to generate
7. Click **Save As Part** to save the generated text
8. Continue with next prompts to build your story
9. Export when complete and convert to speech in TTS tab

**Available Templates**:
- `StoryTemplates/HorrorTemplate.md` - Psychological/supernatural horror
- `StoryTemplates/DeepWebTemplate.md` - Cyber-horror and tech mysteries
- `StoryTemplates/AlienTemplate.md` - Sci-fi horror and cosmic dread
- `StoryTemplates/AnachronismTemplate.md` - Time loops and temporal horror

**Example Projects**:
- `ExampleProjects/Horror_Example_TheNightShift/` - Complete 15-min horror example

**Full Documentation**:
- `STORY_BUILDER_QUICK_START.md` - Beginner's guide
- `STORY_BUILDER_README.md` - Technical details
- `README_StoryBuilderV2.md` - Advanced features

## File Formats

### Supported Input Formats
- **Plain Text** (.txt)
- **Markdown** (.md)
- **CSV Files** (.csv)
- **Direct Text Entry**

### Supported Output Formats
- **WAV**: Uncompressed, highest quality
- **MP3**: Compressed, good quality, smaller files
- **OGG**: Open source compressed format

### Quality Settings
- **Standard**: 64 kbps, 22 kHz - Good for voice content (uses 96k output format)
- **High**: 128 kbps, 44 kHz - Better quality for music/premium content (uses 192k output format)
- **Premium**: 320 kbps, 48 kHz - Highest quality, larger files (uses 192k output format - highest available in Azure Speech SDK)

## Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

### Clone and Build
```bash
# Clone the repository
git clone https://github.com/Saiyan9001/azure-tts-batch-studio.git
cd azure-tts-batch-studio

# Restore dependencies
dotnet restore AzureTtsBatchStudio/AzureTtsBatchStudio.csproj

# Build the application
dotnet build AzureTtsBatchStudio/AzureTtsBatchStudio.csproj --configuration Release

# Run the application
dotnet run --project AzureTtsBatchStudio/AzureTtsBatchStudio.csproj
```

### Build Packages
```bash
# Build portable version
dotnet publish AzureTtsBatchStudio/AzureTtsBatchStudio.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish/win-x64

# Create installer (requires Inno Setup)
# Use the provided setup.iss script

# Create MSIX package (requires Windows SDK)
# Use the provided Package.appxmanifest
```

## Architecture

### Technologies Used
- **Framework**: .NET 8.0
- **UI Framework**: Avalonia UI 11.3.5
- **MVVM**: CommunityToolkit.Mvvm
- **Azure SDK**: Microsoft.CognitiveServices.Speech
- **Packaging**: MSIX, Inno Setup

### Project Structure
```
AzureTtsBatchStudio/
├── Models/              # Data models and DTOs
├── Services/            # Business logic and Azure integration
├── ViewModels/          # MVVM view models
├── Views/               # UI views and controls
├── Assets/              # Icons, images, and resources
└── AzureTtsBatchStudio.csproj
```

### Key Components
- **AzureTtsService**: Handles Azure Speech Services integration
- **SettingsService**: Manages application configuration
- **MainWindowViewModel**: Primary UI logic and state management
- **TtsModels**: Data structures for voice, language, and request handling

## CI/CD Pipeline

The project uses GitHub Actions for automated building and releasing:

### Workflows
- **Build and Test**: Runs on every push and pull request
- **Release**: Creates installers and packages on version tags
- **Code Quality**: Runs static analysis and security checks

### Supported Platforms
- **Windows x64**: Primary target platform
- **Windows x86**: Legacy 32-bit support
- **Windows ARM64**: Modern ARM devices

### Artifacts Generated
- Windows installer (.exe)
- MSIX package (.msix)
- Portable ZIP archives
- Debug symbols and documentation

## Contributing

### Getting Started
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

### Code Standards
- Follow C# coding conventions
- Use async/await for I/O operations
- Implement proper error handling
- Add XML documentation for public APIs
- Maintain MVVM separation of concerns

### Reporting Issues
- Use the GitHub issue tracker
- Include detailed reproduction steps
- Provide system information
- Attach log files if available

## Troubleshooting

### Common Issues

**"Azure credentials not configured"**
- Verify your subscription key and region in Settings
- Ensure your Azure Speech Services resource is active
- Check network connectivity

**"Voice not found"**
- The selected voice may not be available in your region
- Try switching to a different voice or language
- Refresh the voice list in Settings

**"File permission errors"**
- Ensure the output directory is writable
- Run as administrator if necessary
- Check disk space availability

**"Audio quality issues"**
- Try different quality settings
- Verify the selected audio format
- Check Azure service quotas

**Windows 11 Specific Issues:**

**"Windows protected your PC" warning**
- Click "More info" then "Run anyway"
- This happens with new unsigned applications
- Add the folder to Windows Defender exceptions if needed

**"App won't start" on Windows 11**
- Ensure you downloaded the x64 version for modern PCs
- Try running as administrator
- Check if .NET runtime is properly installed

**"Missing DLL" errors**
- Use the self-contained portable version (includes all dependencies)
- Avoid extracting to OneDrive/cloud folders initially
- Make sure antivirus isn't blocking files

### Getting Help
- Review the [FAQ](https://github.com/Saiyan9001/azure-tts-batch-studio/wiki/FAQ)
- Search existing [issues](https://github.com/Saiyan9001/azure-tts-batch-studio/issues)
- Create a new issue with detailed information
- Join community discussions

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **Microsoft Azure**: For providing excellent Speech Services
- **Avalonia UI**: For the cross-platform UI framework
- **Community Contributors**: For testing, feedback, and contributions
- **.NET Team**: For the robust development platform

## Support

### Commercial Support
For commercial support, custom development, or enterprise licensing, please contact [support@azurettsbatchstudio.com](mailto:support@azurettsbatchstudio.com).

### Community Support
- **GitHub Issues**: Bug reports and feature requests
- **Discussions**: General questions and community help
- **Wiki**: Documentation and guides

---

**Made with ❤️ for the developer community**

*Azure TTS Batch Studio - Bringing voice to your text, one batch at a time.*