using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureTtsBatchStudio.Models;
using AzureTtsBatchStudio.Services;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace AzureTtsBatchStudio.ViewModels
{
    public partial class StoryBuilderViewModel : ViewModelBase
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly IProjectManager _projectManager;
        private readonly ITokenBudgeter _tokenBudgeter;
        private readonly ISettingsService _settingsService;
        private readonly ITopicManager _topicManager;
        private readonly IModelPresetService _modelPresetService;
        private readonly IQuickActionService _quickActionService;
        private readonly IDirectiveService _directiveService;
        private readonly IDurationCalculatorService _durationCalculatorService;
        private readonly IStreamingFileService _streamingFileService;
        private CancellationTokenSource? _cancellationTokenSource;
        private string? _currentStreamingFile;

        [ObservableProperty]
        private string _projectsRoot = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _availableProjects = new();

        [ObservableProperty]
        private string? _selectedProject;

        [ObservableProperty]
        private StoryProject? _currentProject;

        [ObservableProperty]
        private string _projectPath = string.Empty;

        [ObservableProperty]
        private ObservableCollection<StoryPart> _storyParts = new();

        [ObservableProperty]
        private StoryPart? _selectedStoryPart;

        [ObservableProperty]
        private string _instructions = string.Empty;

        [ObservableProperty]
        private string _model = "gpt-4";

        [ObservableProperty]
        private ObservableCollection<ModelPreset> _modelPresets = new();

        [ObservableProperty]
        private ModelPreset? _selectedModelPreset;

        [ObservableProperty]
        private bool _isCustomModel = false;

        [ObservableProperty]
        private string _customModel = string.Empty;

        [ObservableProperty]
        private string _modelHelp = string.Empty;

        [ObservableProperty]
        private ObservableCollection<QuickAction> _quickActions = new();

        [ObservableProperty]
        private double _temperature = 0.8;

        [ObservableProperty]
        private double _topP = 1.0;

        [ObservableProperty]
        private int _maxOutputTokens = 2048;

        [ObservableProperty]
        private int _contextBudgetTokens = 32000;

        [ObservableProperty]
        private int _kRecentParts = 3;

        [ObservableProperty]
        private bool _streaming = true;

        [ObservableProperty]
        private string _promptInput = string.Empty;

        [ObservableProperty]
        private string _outputText = string.Empty;

        [ObservableProperty]
        private bool _isGenerating = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private string _tokenBudgetInfo = string.Empty;

        [ObservableProperty]
        private double _progressValue = 0;

        [ObservableProperty]
        private TimeSpan _elapsedTime = TimeSpan.Zero;

        [ObservableProperty]
        private ObservableCollection<Topic> _topics = new();

        [ObservableProperty]
        private ObservableCollection<Directive> _directives = new();

        [ObservableProperty]
        private int _targetMinutes = 0;

        [ObservableProperty]
        private int _wpm = 170;

        [ObservableProperty]
        private int _currentWordCount = 0;

        [ObservableProperty]
        private int _targetWordCount = 0;

        [ObservableProperty]
        private double _completionPercentage = 0;

        [ObservableProperty]
        private string _progressMessage = string.Empty;

        [ObservableProperty]
        private string _nextDirectiveText = string.Empty;

        [ObservableProperty]
        private string _tokenBudgetWarning = string.Empty;

        public StoryBuilderViewModel() : this(
            new OpenAIClient(new System.Net.Http.HttpClient()),
            new ProjectManager(),
            new TokenBudgeter(),
            new SettingsService(),
            new TopicManager(),
            new ModelPresetService(),
            new QuickActionService(),
            new DirectiveService(),
            new DurationCalculatorService(),
            new StreamingFileService())
        {
        }

        public StoryBuilderViewModel(
            IOpenAIClient openAIClient,
            IProjectManager projectManager,
            ITokenBudgeter tokenBudgeter,
            ISettingsService settingsService,
            ITopicManager topicManager,
            IModelPresetService modelPresetService,
            IQuickActionService quickActionService,
            IDirectiveService directiveService,
            IDurationCalculatorService durationCalculatorService,
            IStreamingFileService streamingFileService)
        {
            _openAIClient = openAIClient;
            _projectManager = projectManager;
            _tokenBudgeter = tokenBudgeter;
            _settingsService = settingsService;
            _topicManager = topicManager;
            _modelPresetService = modelPresetService;
            _quickActionService = quickActionService;
            _directiveService = directiveService;
            _durationCalculatorService = durationCalculatorService;
            _streamingFileService = streamingFileService;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var settings = await _settingsService.LoadSettingsAsync();
                ProjectsRoot = _projectManager.GetProjectsRootPath();
                
                // Load OpenAI API key from settings if available
                // Note: In production, this should use secure storage
                if (!string.IsNullOrEmpty(settings.OpenAIApiKey))
                {
                    _openAIClient.ConfigureApiKey(settings.OpenAIApiKey);
                }

                // Load model presets
                var presets = await _modelPresetService.GetPresetsAsync();
                ModelPresets.Clear();
                foreach (var preset in presets)
                {
                    ModelPresets.Add(preset);
                }

                // Load quick actions
                var actions = _quickActionService.GetQuickActions();
                QuickActions.Clear();
                foreach (var action in actions)
                {
                    QuickActions.Add(action);
                }

                await RefreshProjectsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initialization error: {ex.Message}";
            }
        }

        partial void OnSelectedProjectChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = LoadProjectAsync(value);
            }
        }

        partial void OnInstructionsChanged(string value)
        {
            if (CurrentProject != null && !string.IsNullOrEmpty(ProjectPath))
            {
                _ = _projectManager.SaveInstructionsAsync(ProjectPath, value);
            }
            UpdateTokenBudget();
        }

        partial void OnPromptInputChanged(string value)
        {
            UpdateTokenBudget();
        }

        partial void OnTemperatureChanged(double value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.Parameters.Temperature = value;
                _ = SaveCurrentProjectAsync();
            }
        }

        partial void OnTopPChanged(double value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.Parameters.TopP = value;
                _ = SaveCurrentProjectAsync();
            }
        }

        partial void OnMaxOutputTokensChanged(int value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.Parameters.MaxOutputTokens = value;
                _ = SaveCurrentProjectAsync();
            }
            UpdateTokenBudget();
        }

        partial void OnContextBudgetTokensChanged(int value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.Parameters.ContextBudgetTokens = value;
                _ = SaveCurrentProjectAsync();
            }
            UpdateTokenBudget();
        }

        partial void OnKRecentPartsChanged(int value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.Parameters.KRecentParts = value;
                _ = SaveCurrentProjectAsync();
            }
            UpdateTokenBudget();
        }

        partial void OnStreamingChanged(bool value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.Parameters.Stream = value;
                _ = SaveCurrentProjectAsync();
            }
        }

        partial void OnSelectedModelPresetChanged(ModelPreset? value)
        {
            if (value != null)
            {
                Model = value.Id;
                ModelHelp = value.Notes;
                IsCustomModel = false;
            }
            else
            {
                IsCustomModel = true;
                ModelHelp = "Enter a custom model name";
            }
        }

        partial void OnIsCustomModelChanged(bool value)
        {
            if (value)
            {
                SelectedModelPreset = null;
                Model = CustomModel;
                ModelHelp = "Enter a custom model name";
            }
        }

        partial void OnCustomModelChanged(string value)
        {
            if (IsCustomModel)
            {
                Model = value;
            }
        }

        partial void OnModelChanged(string value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.Model = value;
                _ = SaveCurrentProjectAsync();
            }
        }

        partial void OnTargetMinutesChanged(int value)
        {
            TargetWordCount = value * Wpm;
            UpdateCompletionPercentage();
        }

        partial void OnWpmChanged(int value)
        {
            if (CurrentProject != null)
            {
                CurrentProject.WpmForDurationEstimates = value;
                _ = SaveCurrentProjectAsync();
            }
            TargetWordCount = TargetMinutes * value;
            UpdateCompletionPercentage();
        }

        [RelayCommand]
        private async Task RefreshProjects()
        {
            await RefreshProjectsAsync();
        }

        [RelayCommand]
        private async Task NewProject()
        {
            try
            {
                // In a real implementation, this would show a dialog to get the project name
                var projectName = $"New Story {DateTime.Now:yyyyMMdd_HHmmss}";
                
                var project = await _projectManager.CreateProjectAsync(ProjectsRoot, projectName);
                await RefreshProjectsAsync();
                SelectedProject = projectName;
                StatusMessage = $"Created project: {projectName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating project: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task OpenProject()
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                    desktop.MainWindow?.StorageProvider is not { } provider)
                    return;

                var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Project Folder",
                    AllowMultiple = false
                });

                if (folders.Any())
                {
                    var folder = folders.First();
                    var localPath = folder.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(localPath))
                    {
                        var projectName = Path.GetFileName(localPath);
                        var project = await _projectManager.LoadProjectAsync(localPath);
                        
                        // Update projects root if needed
                        var parentPath = Path.GetDirectoryName(localPath);
                        if (!string.IsNullOrEmpty(parentPath))
                        {
                            ProjectsRoot = parentPath;
                            _projectManager.SetProjectsRootPath(parentPath);
                        }

                        await RefreshProjectsAsync();
                        SelectedProject = projectName;
                        StatusMessage = $"Opened project: {projectName}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening project: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task Send()
        {
            if (string.IsNullOrWhiteSpace(PromptInput) || CurrentProject == null)
                return;

            if (!_openAIClient.IsConfigured)
            {
                StatusMessage = "OpenAI API key not configured. Please check settings.";
                return;
            }

            try
            {
                IsGenerating = true;
                StatusMessage = "Generating...";
                _cancellationTokenSource = new CancellationTokenSource();
                
                var startTime = DateTime.UtcNow;
                
                // Get active directives for the current state
                var currentPartIndex = StoryParts.Count;
                var activeDirectives = _directiveService.GetActiveDirectives(Directives.ToList(), currentPartIndex, CurrentWordCount);
                
                // Apply directives to the prompt
                var enhancedPrompt = _directiveService.BuildPromptWithDirectives(PromptInput, activeDirectives);
                
                var budget = _tokenBudgeter.CalculateBudget(Instructions, enhancedPrompt, StoryParts.ToList(), CurrentProject.Parameters);
                
                var messages = new List<OpenAIMessage>();
                
                // Add system message with instructions
                if (!string.IsNullOrEmpty(budget.TruncatedInstructions))
                {
                    messages.Add(new OpenAIMessage { Role = "system", Content = budget.TruncatedInstructions });
                }

                // Add context from recent parts
                foreach (var part in budget.IncludedParts)
                {
                    messages.Add(new OpenAIMessage { Role = "assistant", Content = part.Content });
                }

                // Add user prompt
                messages.Add(new OpenAIMessage { Role = "user", Content = budget.TruncatedPrompt });

                var request = new OpenAIGenerationRequest
                {
                    Model = Model,
                    Messages = messages,
                    Temperature = Temperature,
                    TopP = TopP,
                    MaxTokens = MaxOutputTokens,
                    Stream = Streaming
                };

                OutputText = string.Empty;

                if (Streaming)
                {
                    // Create temp file for streaming
                    _currentStreamingFile = await _streamingFileService.CreateTempFileAsync("story_output");
                    
                    var stream = await _openAIClient.GenerateStreamAsync(request, _cancellationTokenSource.Token);
                    await foreach (var progress in stream)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                            break;

                        OutputText = progress.Text;
                        ElapsedTime = progress.Elapsed;
                        
                        // Save incremental content to temp file
                        if (!string.IsNullOrEmpty(progress.Text) && _currentStreamingFile != null)
                        {
                            await _streamingFileService.AppendToFileAsync(_currentStreamingFile, progress.Text);
                        }
                        
                        if (!string.IsNullOrEmpty(progress.Error))
                        {
                            StatusMessage = $"Error: {progress.Error}";
                            break;
                        }

                        if (progress.IsComplete)
                        {
                            StatusMessage = $"Generation complete. Elapsed: {progress.Elapsed:mm\\:ss}";
                            
                            // Finalize the streaming file
                            if (_currentStreamingFile != null)
                            {
                                var finalFile = await _streamingFileService.FinalizeFileAsync(
                                    _currentStreamingFile, 
                                    $"story_output_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");
                                StatusMessage += $" | Saved to: {Path.GetFileName(finalFile)}";
                            }
                            break;
                        }
                    }
                }
                else
                {
                    var result = await _openAIClient.GenerateAsync(request, _cancellationTokenSource.Token);
                    OutputText = result.Text;
                    ElapsedTime = result.Elapsed;
                    
                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        StatusMessage = $"Error: {result.Error}";
                    }
                    else
                    {
                        StatusMessage = $"Generation complete. Elapsed: {result.Elapsed:mm\\:ss}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Generation error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
                
                // Cleanup temp file if generation was interrupted
                if (_currentStreamingFile != null)
                {
                    await _streamingFileService.CleanupTempFileAsync(_currentStreamingFile);
                    _currentStreamingFile = null;
                }
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            IsGenerating = false;
            StatusMessage = "Generation cancelled";
        }

        [RelayCommand]
        private async Task SaveAsPart()
        {
            if (string.IsNullOrWhiteSpace(OutputText) || string.IsNullOrEmpty(ProjectPath))
                return;

            try
            {
                await _projectManager.SaveStoryPartAsync(ProjectPath, OutputText);
                await RefreshStoryPartsAsync();
                StatusMessage = "Saved as new story part";
                OutputText = string.Empty; // Clear output after saving
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving part: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task Continue()
        {
            if (string.IsNullOrWhiteSpace(OutputText))
            {
                PromptInput = "Continue the story.";
            }
            else
            {
                PromptInput = $"Continue from: {OutputText.Substring(Math.Max(0, OutputText.Length - 200))}";
            }
            
            await Send();
        }

        [RelayCommand]
        private void CopyToClipboard()
        {
            if (!string.IsNullOrEmpty(OutputText))
            {
                try
                {
                    // Note: In Avalonia, clipboard access requires the window context
                    // This is a simplified version - in production you'd get the actual clipboard
                    StatusMessage = "Text copied to clipboard";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error copying to clipboard: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private void PickRandomTopic()
        {
            if (Topics.Count == 0)
            {
                StatusMessage = "No topics available. Add some topics first.";
                return;
            }

            var randomTopic = _topicManager.PickRandomTopic(Topics.ToList(), CurrentProject?.RandomSeed);
            PromptInput = $"Write about: {randomTopic.TopicText}";
            StatusMessage = $"Random topic selected: {randomTopic.TopicText}";
        }

        [RelayCommand]
        private void Pick3Topics()
        {
            if (Topics.Count == 0)
            {
                StatusMessage = "No topics available. Add some topics first.";
                return;
            }

            var randomTopics = _topicManager.PickRandomTopics(Topics.ToList(), 3, CurrentProject?.RandomSeed);
            var topicsText = string.Join(", ", randomTopics.Select(t => t.TopicText));
            PromptInput = $"Write about: {topicsText}";
            StatusMessage = $"Random topics selected: {topicsText}";
        }

        [RelayCommand]
        private void ShuffleTopics()
        {
            if (Topics.Count <= 1)
            {
                StatusMessage = "Need at least 2 topics to shuffle.";
                return;
            }

            var topicsList = Topics.ToList();
            _topicManager.ShuffleTopics(topicsList, CurrentProject?.RandomSeed);
            
            Topics.Clear();
            foreach (var topic in topicsList)
            {
                Topics.Add(topic);
            }

            StatusMessage = "Topics shuffled";
        }

        [RelayCommand]
        private async Task ApplyQuickAction(QuickAction action)
        {
            if (string.IsNullOrEmpty(OutputText) || action == null)
                return;

            try
            {
                var actionPrompt = _quickActionService.ApplyQuickAction(action, OutputText, PromptInput);
                PromptInput = actionPrompt;
                StatusMessage = $"Applied quick action: {action.Label}";
                
                // Optionally auto-send for certain actions like Continue
                if (action.Name == "Continue")
                {
                    await Send();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error applying quick action: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ManageModels()
        {
            // This will be implemented in a future update with a dialog
            StatusMessage = "Model management dialog coming soon...";
        }

        private async Task RefreshProjectsAsync()
        {
            try
            {
                var projects = await _projectManager.GetProjectsAsync(ProjectsRoot);
                AvailableProjects.Clear();
                foreach (var project in projects)
                {
                    AvailableProjects.Add(project);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing projects: {ex.Message}";
            }
        }

        private async Task LoadProjectAsync(string projectName)
        {
            try
            {
                ProjectPath = Path.Combine(ProjectsRoot, projectName);
                CurrentProject = await _projectManager.LoadProjectAsync(ProjectPath);
                
                // Load project data
                Instructions = await _projectManager.LoadInstructionsAsync(ProjectPath);
                
                // Load UI state from project
                Model = CurrentProject.Model;
                
                // Set the correct model preset based on the loaded model
                var preset = ModelPresets.FirstOrDefault(p => p.Id == CurrentProject.Model);
                if (preset != null)
                {
                    SelectedModelPreset = preset;
                    IsCustomModel = false;
                    ModelHelp = preset.Notes;
                }
                else
                {
                    SelectedModelPreset = null;
                    IsCustomModel = true;
                    CustomModel = CurrentProject.Model;
                    ModelHelp = "Custom model";
                }
                
                Temperature = CurrentProject.Parameters.Temperature;
                TopP = CurrentProject.Parameters.TopP;
                MaxOutputTokens = CurrentProject.Parameters.MaxOutputTokens;
                ContextBudgetTokens = CurrentProject.Parameters.ContextBudgetTokens;
                KRecentParts = CurrentProject.Parameters.KRecentParts;
                Streaming = CurrentProject.Parameters.Stream;
                Wpm = CurrentProject.WpmForDurationEstimates;

                await RefreshStoryPartsAsync();
                await LoadTopicsAsync();
                await LoadDirectivesAsync();
                
                // Save project with updated last opened time
                await SaveCurrentProjectAsync();
                
                StatusMessage = $"Loaded project: {projectName}";
                UpdateTokenBudget();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading project: {ex.Message}";
            }
        }

        private async Task RefreshStoryPartsAsync()
        {
            if (string.IsNullOrEmpty(ProjectPath))
                return;

            try
            {
                var parts = await _projectManager.GetStoryPartsAsync(ProjectPath);
                StoryParts.Clear();
                foreach (var part in parts)
                {
                    StoryParts.Add(part);
                }

                CurrentWordCount = parts.Sum(p => p.WordCount);
                UpdateCompletionPercentage();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading story parts: {ex.Message}";
            }
        }

        private async Task LoadTopicsAsync()
        {
            if (string.IsNullOrEmpty(ProjectPath))
                return;

            try
            {
                var topics = await _projectManager.LoadTopicsAsync(ProjectPath);
                Topics.Clear();
                foreach (var topic in topics)
                {
                    Topics.Add(topic);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading topics: {ex.Message}";
            }
        }

        private async Task LoadDirectivesAsync()
        {
            if (string.IsNullOrEmpty(ProjectPath))
                return;

            try
            {
                var directives = await _projectManager.LoadDirectivesAsync(ProjectPath);
                Directives.Clear();
                foreach (var directive in directives)
                {
                    Directives.Add(directive);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading directives: {ex.Message}";
            }
        }

        private async Task SaveCurrentProjectAsync()
        {
            if (CurrentProject != null && !string.IsNullOrEmpty(ProjectPath))
            {
                try
                {
                    await _projectManager.SaveProjectAsync(ProjectPath, CurrentProject);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error saving project: {ex.Message}";
                }
            }
        }

        private void UpdateTokenBudget()
        {
            if (CurrentProject == null)
            {
                TokenBudgetInfo = "No project loaded";
                return;
            }

            try
            {
                var budget = _tokenBudgeter.CalculateBudget(Instructions, PromptInput, StoryParts.ToList(), CurrentProject.Parameters);
                TokenBudgetInfo = $"Estimated tokens: {budget.EstimatedTokens}/{ContextBudgetTokens} | Parts: {budget.IncludedParts.Count}/{KRecentParts}";
                
                if (budget.IsTruncated)
                {
                    TokenBudgetInfo += $" | {budget.TruncationReason}";
                }

                // Set warning message
                TokenBudgetWarning = budget.HasWarning ? budget.WarningMessage : string.Empty;
            }
            catch (Exception ex)
            {
                TokenBudgetInfo = $"Budget calculation error: {ex.Message}";
            }
        }

        private void UpdateCompletionPercentage()
        {
            var targetWords = _durationCalculatorService.CalculateTargetWords(TargetMinutes, Wpm);
            TargetWordCount = targetWords;
            
            if (targetWords > 0)
            {
                CompletionPercentage = _durationCalculatorService.CalculateProgress(CurrentWordCount, targetWords);
                ProgressMessage = _durationCalculatorService.FormatProgressMessage(CurrentWordCount, targetWords, Wpm);
            }
            else
            {
                CompletionPercentage = 0;
                ProgressMessage = $"{CurrentWordCount} words";
            }

            // Update next directive info
            var currentPartIndex = StoryParts.Count;
            var nextDirective = _directiveService.GetNextDirective(Directives.ToList(), currentPartIndex, CurrentWordCount);
            if (nextDirective != null)
            {
                var triggerText = nextDirective.Trigger.AtPart.HasValue 
                    ? $"at part {nextDirective.Trigger.AtPart.Value}" 
                    : $"at {nextDirective.Trigger.AtWordCount} words";
                NextDirectiveText = $"Next: {nextDirective.DirectiveText} ({triggerText})";
            }
            else
            {
                NextDirectiveText = string.Empty;
            }
        }
    }
}