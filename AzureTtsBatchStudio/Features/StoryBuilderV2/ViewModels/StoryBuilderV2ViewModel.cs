using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AzureTtsBatchStudio.Features.StoryBuilderV2.Models;
using AzureTtsBatchStudio.Features.StoryBuilderV2.Services;
using AzureTtsBatchStudio.Infrastructure.Llm;
using AzureTtsBatchStudio.ViewModels;

namespace AzureTtsBatchStudio.Features.StoryBuilderV2.ViewModels
{
    /// <summary>
    /// ViewModel for Story Builder V2
    /// </summary>
    public partial class StoryBuilderV2ViewModel : ViewModelBase
    {
        private readonly ILlmService _llmService;
        private readonly IProjectStore _projectStore;
        private readonly ITemplateEngine _templateEngine;
        private readonly ITemplateManager _templateManager;
        private readonly ISsmlBuilder _ssmlBuilder;
        private readonly IDurationEstimator _durationEstimator;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private string _projectsRoot = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _availableProjects = new();

        [ObservableProperty]
        private string? _selectedProjectPath;

        [ObservableProperty]
        private StoryProject? _currentProject;

        [ObservableProperty]
        private ObservableCollection<StoryBeat> _beats = new();

        [ObservableProperty]
        private StoryBeat? _selectedBeat;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isGenerating;

        [ObservableProperty]
        private string _outputText = string.Empty;

        [ObservableProperty]
        private double _totalCostUsd;

        [ObservableProperty]
        private int _totalTokens;

        [ObservableProperty]
        private string _llmProvider = "OpenAI";

        [ObservableProperty]
        private string _llmModel = "gpt-4";

        [ObservableProperty]
        private double _temperature = 0.8;

        [ObservableProperty]
        private int _maxTokens = 4000;

        public StoryBuilderV2ViewModel(
            ILlmService llmService,
            IProjectStore projectStore,
            ITemplateEngine templateEngine,
            ITemplateManager templateManager,
            ISsmlBuilder ssmlBuilder,
            IDurationEstimator durationEstimator)
        {
            _llmService = llmService;
            _projectStore = projectStore;
            _templateEngine = templateEngine;
            _templateManager = templateManager;
            _ssmlBuilder = ssmlBuilder;
            _durationEstimator = durationEstimator;

            // Set default projects root
            ProjectsRoot = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "StoryBuilder Projects");

            _ = InitializeAsync();
        }

        // Parameterless constructor for design-time support
        public StoryBuilderV2ViewModel() : this(
            new OpenAiLlmService(new LlmOptions()),
            new FileProjectStore(),
            new TemplateEngine(),
            new TemplateManager(),
            new SsmlBuilder(),
            new DurationEstimator())
        {
        }

        private async Task InitializeAsync()
        {
            await RefreshProjectsAsync();
        }

        [RelayCommand]
        private async Task RefreshProjects()
        {
            await RefreshProjectsAsync();
        }

        private async Task RefreshProjectsAsync()
        {
            var result = await _projectStore.ListProjectsAsync(ProjectsRoot);
            if (result.IsSuccess && result.Value != null)
            {
                AvailableProjects.Clear();
                foreach (var project in result.Value)
                {
                    AvailableProjects.Add(project);
                }
                StatusMessage = $"Found {result.Value.Count} project(s)";
            }
            else
            {
                StatusMessage = result.Error;
            }
        }

        [RelayCommand]
        private async Task CreateProject()
        {
            var projectName = $"Horror Story {DateTime.Now:yyyy-MM-dd HHmm}";
            var result = await _projectStore.CreateProjectAsync(ProjectsRoot, projectName);
            
            if (result.IsSuccess && result.Value != null)
            {
                StatusMessage = $"Created project: {projectName}";
                await RefreshProjectsAsync();
                SelectedProjectPath = result.Value;
                await LoadProject();
            }
            else
            {
                StatusMessage = $"Failed to create project: {result.Error}";
            }
        }

        [RelayCommand]
        private async Task LoadProject()
        {
            if (string.IsNullOrEmpty(SelectedProjectPath))
                return;

            var result = await _projectStore.LoadAsync(SelectedProjectPath);
            
            if (result.IsSuccess && result.Value != null)
            {
                CurrentProject = result.Value;
                Beats.Clear();
                foreach (var beat in CurrentProject.Beats.OrderBy(b => b.OrderIndex))
                {
                    Beats.Add(beat);
                }
                
                TotalCostUsd = CurrentProject.Metrics.TotalCostUsd;
                TotalTokens = CurrentProject.Metrics.TotalPromptTokens + CurrentProject.Metrics.TotalCompletionTokens;
                
                StatusMessage = $"Loaded: {CurrentProject.Title}";
            }
            else
            {
                StatusMessage = $"Failed to load project: {result.Error}";
            }
        }

        [RelayCommand]
        private async Task GenerateOutline()
        {
            if (CurrentProject == null || SelectedProjectPath == null)
            {
                StatusMessage = "No project loaded";
                return;
            }

            if (!_llmService.IsConfigured)
            {
                StatusMessage = "LLM service not configured. Please set API key in settings.";
                return;
            }

            try
            {
                IsGenerating = true;
                StatusMessage = "Generating outline...";
                _cancellationTokenSource = new CancellationTokenSource();

                // Prepare template variables
                var variables = new Dictionary<string, object>
                {
                    ["title"] = CurrentProject.Title,
                    ["genre"] = CurrentProject.Genre,
                    ["duration"] = CurrentProject.Target,
                    ["style"] = CurrentProject.Style
                };

                // Get outline template
                var template = _templateManager.GetTemplate("Outline");
                var prompt = _templateEngine.Render(template, variables);

                // Generate outline
                var request = new LlmRequest
                {
                    SystemPrompt = "You are a professional horror story writer.",
                    UserPrompt = prompt,
                    Temperature = Temperature,
                    MaxTokens = MaxTokens
                };

                var response = await _llmService.CompleteAsync(request, _cancellationTokenSource.Token);
                
                OutputText = response.Text;
                TotalCostUsd += response.EstimatedCostUsd;
                TotalTokens += response.TotalTokens;

                StatusMessage = $"Outline generated. Cost: ${response.EstimatedCostUsd:F4}, Tokens: {response.TotalTokens}";

                // Update metrics
                var updatedMetrics = CurrentProject.Metrics with
                {
                    TotalPromptTokens = CurrentProject.Metrics.TotalPromptTokens + response.PromptTokens,
                    TotalCompletionTokens = CurrentProject.Metrics.TotalCompletionTokens + response.CompletionTokens,
                    TotalCostUsd = TotalCostUsd,
                    GenerationCount = CurrentProject.Metrics.GenerationCount + 1
                };

                CurrentProject = CurrentProject with { Metrics = updatedMetrics };
                await _projectStore.SaveAsync(CurrentProject, SelectedProjectPath);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private async Task DraftBeat()
        {
            if (CurrentProject == null || SelectedBeat == null || SelectedProjectPath == null)
            {
                StatusMessage = "No beat selected";
                return;
            }

            if (!_llmService.IsConfigured)
            {
                StatusMessage = "LLM service not configured. Please set API key in settings.";
                return;
            }

            try
            {
                IsGenerating = true;
                StatusMessage = $"Drafting beat: {SelectedBeat.Title}...";
                _cancellationTokenSource = new CancellationTokenSource();

                // Prepare template variables
                var variables = new Dictionary<string, object>
                {
                    ["title"] = CurrentProject.Title,
                    ["style"] = CurrentProject.Style,
                    ["characters"] = string.Join(", ", CurrentProject.Characters.Select(c => c.Name)),
                    ["beat"] = SelectedBeat
                };

                // Get beat draft template
                var template = _templateManager.GetTemplate("BeatDraft");
                var prompt = _templateEngine.Render(template, variables);

                // Generate beat draft
                var request = new LlmRequest
                {
                    SystemPrompt = "You are a professional horror story writer.",
                    UserPrompt = prompt,
                    Temperature = Temperature,
                    MaxTokens = MaxTokens
                };

                var response = await _llmService.CompleteAsync(request, _cancellationTokenSource.Token);
                
                OutputText = response.Text;
                TotalCostUsd += response.EstimatedCostUsd;
                TotalTokens += response.TotalTokens;

                // Update beat with draft
                var wordCount = _durationEstimator.CountWords(response.Text);
                var estimatedMinutes = _durationEstimator.EstimateDurationMinutes(wordCount, CurrentProject.Target.TargetWpm);

                var updatedBeat = SelectedBeat with
                {
                    DraftMd = response.Text,
                    Status = BeatStatus.Drafted,
                    EstimatedMinutes = estimatedMinutes
                };

                // Update project
                var beatIndex = CurrentProject.Beats.FindIndex(b => b.Id == SelectedBeat.Id);
                if (beatIndex >= 0)
                {
                    var updatedBeats = CurrentProject.Beats.ToList();
                    updatedBeats[beatIndex] = updatedBeat;

                    var updatedMetrics = CurrentProject.Metrics with
                    {
                        TotalPromptTokens = CurrentProject.Metrics.TotalPromptTokens + response.PromptTokens,
                        TotalCompletionTokens = CurrentProject.Metrics.TotalCompletionTokens + response.CompletionTokens,
                        TotalCostUsd = TotalCostUsd,
                        GenerationCount = CurrentProject.Metrics.GenerationCount + 1
                    };

                    CurrentProject = CurrentProject with
                    {
                        Beats = updatedBeats,
                        Metrics = updatedMetrics
                    };

                    await _projectStore.SaveAsync(CurrentProject, SelectedProjectPath);
                    await LoadProject(); // Refresh
                }

                StatusMessage = $"Beat drafted. Cost: ${response.EstimatedCostUsd:F4}, Words: {wordCount}, Duration: {estimatedMinutes:F1} min";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            IsGenerating = false;
            StatusMessage = "Cancelled";
        }

        [RelayCommand]
        private async Task TestConnection()
        {
            StatusMessage = "Testing LLM connection...";
            
            try
            {
                var result = await _llmService.TestConnectionAsync();
                StatusMessage = result ? "Connection successful!" : "Connection failed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection error: {ex.Message}";
            }
        }
    }
}
