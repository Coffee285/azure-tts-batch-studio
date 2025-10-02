using System;
using System.Collections.Generic;

namespace AzureTtsBatchStudio.Features.StoryBuilderV2.Models
{
    /// <summary>
    /// Story beat status
    /// </summary>
    public enum BeatStatus
    {
        Empty,      // Not yet drafted
        Drafted,    // Initial draft complete
        Refined,    // Refined version available
        Locked      // Final version, locked from editing
    }

    /// <summary>
    /// Complete story project with all metadata and content
    /// </summary>
    public record StoryProject
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Title { get; init; } = string.Empty;
        public string Genre { get; init; } = "Horror";
        public StoryStyleGuide Style { get; init; } = new();
        public List<Character> Characters { get; init; } = new();
        public List<StoryBeat> Beats { get; init; } = new();
        public TargetDuration Target { get; init; } = new();
        public TtsProfile Tts { get; init; } = new();
        public AudioDesign Audio { get; init; } = new();
        public ProjectMetrics Metrics { get; init; } = new();
        public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// A single story beat (scene/section)
    /// </summary>
    public record StoryBeat
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Title { get; init; } = string.Empty;
        public string Prompt { get; init; } = string.Empty;
        public string DraftMd { get; init; } = string.Empty;
        public BeatStatus Status { get; init; } = BeatStatus.Empty;
        public double EstimatedMinutes { get; init; }
        public List<InlineSfxCue> Sfx { get; init; } = new();
        public int OrderIndex { get; init; }
    }

    /// <summary>
    /// Character definition with voice and style
    /// </summary>
    public record Character
    {
        public string Name { get; init; } = string.Empty;
        public string VoiceHint { get; init; } = string.Empty;
        public string Bio { get; init; } = string.Empty;
        public string SpeakingStyle { get; init; } = string.Empty;
        public string SafetyNotes { get; init; } = string.Empty;
    }

    /// <summary>
    /// Story style guide and constraints
    /// </summary>
    public record StoryStyleGuide
    {
        public string NarrativeVoice { get; init; } = "Third-person omniscient";
        public string ContentConstraints { get; init; } = "No graphic violence, no sexual content, no realistic self-harm instructions";
        public string TropesToAvoid { get; init; } = string.Empty;
        public string ToneAnchors { get; init; } = "Unsettling, claustrophobic, slow-burn";
        public string ExampleExcerpts { get; init; } = string.Empty;
    }

    /// <summary>
    /// Target duration and pacing
    /// </summary>
    public record TargetDuration
    {
        public int MinMinutes { get; init; } = 15;
        public int MaxMinutes { get; init; } = 45;
        public int TargetWpm { get; init; } = 130;
    }

    /// <summary>
    /// TTS configuration and voice mapping
    /// </summary>
    public record TtsProfile
    {
        public string DefaultVoice { get; init; } = "en-US-AvaNeural";
        public double Rate { get; init; } = 1.0;
        public double Pitch { get; init; } = 0.0;
        public double Volume { get; init; } = 0.0;
        public Dictionary<string, string> CharacterVoiceMap { get; init; } = new();
        public bool UseNeural { get; init; } = true;
        public bool UseMultiVoice { get; init; } = false;
        public bool UseEmotion { get; init; } = true;
    }

    /// <summary>
    /// Audio design and sound effects
    /// </summary>
    public record AudioDesign
    {
        public string BackgroundTrackPath { get; init; } = string.Empty;
        public double BgVolumeDb { get; init; } = -20.0;
        public List<SfxLibraryItem> Library { get; init; } = new();
        public bool DuckMusicDuringSpeech { get; init; } = true;
        public double DuckDb { get; init; } = -15.0;
    }

    /// <summary>
    /// Sound effect library item
    /// </summary>
    public record SfxLibraryItem
    {
        public string Key { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public double DefaultVolumeDb { get; init; } = 0.0;
    }

    /// <summary>
    /// Inline sound effect cue in a beat
    /// </summary>
    public record InlineSfxCue
    {
        public TimeSpan? At { get; init; }
        public string? OnWord { get; init; }
        public string SfxKey { get; init; } = string.Empty;
        public double VolumeDb { get; init; } = 0.0;
    }

    /// <summary>
    /// Project metrics for cost and token tracking
    /// </summary>
    public record ProjectMetrics
    {
        public int TotalPromptTokens { get; init; }
        public int TotalCompletionTokens { get; init; }
        public double TotalCostUsd { get; init; }
        public TimeSpan TotalGenerationTime { get; init; }
        public int GenerationCount { get; init; }
    }
}
