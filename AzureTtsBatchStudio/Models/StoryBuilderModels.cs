using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzureTtsBatchStudio.Models
{
    public class StoryProject
    {
        public int Version { get; set; } = 1;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastOpened { get; set; } = DateTime.UtcNow;
        public string Model { get; set; } = "gpt-4";
        public ModelParameters Parameters { get; set; } = new();
        public int WpmForDurationEstimates { get; set; } = 170;
        public int? RandomSeed { get; set; }
        public string TopicBankFile { get; set; } = "scratch/topics.json";
        public string DirectiveTimelineFile { get; set; } = "scratch/directives.json";
        public UiSettings Ui { get; set; } = new();
    }

    public class ModelParameters
    {
        public double Temperature { get; set; } = 0.8;
        public double TopP { get; set; } = 1.0;
        public int MaxOutputTokens { get; set; } = 2048;
        public bool Stream { get; set; } = true;
        public int ContextBudgetTokens { get; set; } = 32000;
        public int KRecentParts { get; set; } = 3;
    }

    public class UiSettings
    {
        public SplitterSettings Splitters { get; set; } = new();
        public string Theme { get; set; } = "dark";
    }

    public class SplitterSettings
    {
        public double Left { get; set; } = 280;
        public double Top { get; set; } = 55;
        public double Bottom { get; set; } = 45;
    }

    public class StoryPart
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public int WordCount { get; set; }
        public int Index { get; set; }
    }

    public class Topic
    {
        public string TopicText { get; set; } = string.Empty;
        public double Weight { get; set; } = 1.0;
    }

    public class Directive
    {
        public DirectiveTrigger Trigger { get; set; } = new();
        public string DirectiveText { get; set; } = string.Empty;
        public bool Strict { get; set; } = false;
    }

    public class DirectiveTrigger
    {
        public int? AtPart { get; set; }
        public int? AtWordCount { get; set; }
    }

    public class GenerationSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string PromptHash { get; set; } = string.Empty;
        public ModelParameters Parameters { get; set; } = new();
        public string Model { get; set; } = string.Empty;
        public TokenUsage? TokenUsage { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string OutputFilePath { get; set; } = string.Empty;
    }

    public class TokenUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class GenerationProgress
    {
        public string Text { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public TokenUsage? TokenUsage { get; set; }
        public TimeSpan Elapsed { get; set; }
        public string? Error { get; set; }
    }

    public class OpenAIGenerationRequest
    {
        public string Model { get; set; } = "gpt-4";
        public List<OpenAIMessage> Messages { get; set; } = new();
        public double Temperature { get; set; } = 0.8;
        public double TopP { get; set; } = 1.0;
        public int MaxTokens { get; set; } = 2048;
        public bool Stream { get; set; } = true;
    }

    public class OpenAIMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class OpenAIStreamEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public List<OpenAIChoice> Choices { get; set; } = new();
        public TokenUsage? Usage { get; set; }
    }

    public class OpenAIChoice
    {
        public int Index { get; set; }
        public OpenAIDelta? Delta { get; set; }
        public string? FinishReason { get; set; }
    }

    public class OpenAIDelta
    {
        public string? Content { get; set; }
    }

    public class ModelPreset
    {
        public string Label { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class ModelPresetsConfig
    {
        public List<ModelPreset> Presets { get; set; } = new();
    }

    public class QuickAction
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}