using FluentValidation;
using AzureTtsBatchStudio.Features.StoryBuilderV2.Models;

namespace AzureTtsBatchStudio.Features.StoryBuilderV2.Validation
{
    /// <summary>
    /// Validator for StoryProject
    /// </summary>
    public class StoryProjectValidator : AbstractValidator<StoryProject>
    {
        public StoryProjectValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title must be 200 characters or less");

            RuleFor(x => x.Genre)
                .NotEmpty().WithMessage("Genre is required");

            RuleFor(x => x.Beats)
                .Must(beats => beats.Count >= 10 && beats.Count <= 20)
                .When(x => x.Beats.Count > 0)
                .WithMessage("Projects should have between 10 and 20 beats for optimal story structure");

            RuleFor(x => x.Target.MinMinutes)
                .InclusiveBetween(15, 45)
                .WithMessage("Minimum duration must be between 15 and 45 minutes");

            RuleFor(x => x.Target.MaxMinutes)
                .InclusiveBetween(15, 45)
                .WithMessage("Maximum duration must be between 15 and 45 minutes");

            RuleFor(x => x.Target.MaxMinutes)
                .GreaterThanOrEqualTo(x => x.Target.MinMinutes)
                .WithMessage("Maximum duration must be greater than or equal to minimum duration");

            RuleFor(x => x.Target.TargetWpm)
                .InclusiveBetween(80, 200)
                .WithMessage("Target WPM must be between 80 and 200");
        }
    }

    /// <summary>
    /// Validator for StoryBeat
    /// </summary>
    public class StoryBeatValidator : AbstractValidator<StoryBeat>
    {
        public StoryBeatValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Beat title is required")
                .MaximumLength(100).WithMessage("Beat title must be 100 characters or less");

            RuleFor(x => x.Prompt)
                .NotEmpty()
                .When(x => x.Status != BeatStatus.Empty)
                .WithMessage("Prompt is required for non-empty beats");

            RuleFor(x => x.DraftMd)
                .NotEmpty()
                .When(x => x.Status == BeatStatus.Drafted || x.Status == BeatStatus.Refined || x.Status == BeatStatus.Locked)
                .WithMessage("Draft content is required for drafted, refined, or locked beats");

            RuleFor(x => x.EstimatedMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Estimated minutes cannot be negative");
        }
    }

    /// <summary>
    /// Validator for TtsProfile
    /// </summary>
    public class TtsProfileValidator : AbstractValidator<TtsProfile>
    {
        public TtsProfileValidator()
        {
            RuleFor(x => x.DefaultVoice)
                .NotEmpty().WithMessage("Default voice is required");

            RuleFor(x => x.Rate)
                .InclusiveBetween(0.5, 2.0)
                .WithMessage("Speech rate must be between 0.5 and 2.0");

            RuleFor(x => x.Pitch)
                .InclusiveBetween(-50, 50)
                .WithMessage("Pitch must be between -50 and 50");

            RuleFor(x => x.Volume)
                .InclusiveBetween(-50, 50)
                .WithMessage("Volume must be between -50 and 50");
        }
    }

    /// <summary>
    /// Validator for AudioDesign
    /// </summary>
    public class AudioDesignValidator : AbstractValidator<AudioDesign>
    {
        public AudioDesignValidator()
        {
            RuleFor(x => x.BgVolumeDb)
                .InclusiveBetween(-60, 6)
                .WithMessage("Background volume must be between -60 and +6 dB");

            RuleFor(x => x.DuckDb)
                .InclusiveBetween(-60, 0)
                .WithMessage("Duck volume must be between -60 and 0 dB");

            RuleFor(x => x.BackgroundTrackPath)
                .Must(path => string.IsNullOrEmpty(path) || System.IO.File.Exists(path))
                .When(x => !string.IsNullOrEmpty(x.BackgroundTrackPath))
                .WithMessage("Background track file does not exist");
        }
    }

    /// <summary>
    /// Validator for Character
    /// </summary>
    public class CharacterValidator : AbstractValidator<Character>
    {
        public CharacterValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Character name is required")
                .MaximumLength(100).WithMessage("Character name must be 100 characters or less");
        }
    }
}
