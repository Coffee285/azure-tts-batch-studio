using System;
using System.Collections.Generic;
using System.Linq;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    public interface IDirectiveService
    {
        List<Directive> GetActiveDirectives(List<Directive> allDirectives, int currentPartIndex, int currentWordCount);
        string BuildPromptWithDirectives(string basePrompt, List<Directive> activeDirectives);
        bool ShouldEnforceDirective(Directive directive, int currentPartIndex, int currentWordCount);
        Directive? GetNextDirective(List<Directive> allDirectives, int currentPartIndex, int currentWordCount);
    }

    public class DirectiveService : IDirectiveService
    {
        public List<Directive> GetActiveDirectives(List<Directive> allDirectives, int currentPartIndex, int currentWordCount)
        {
            var activeDirectives = new List<Directive>();

            foreach (var directive in allDirectives)
            {
                if (ShouldEnforceDirective(directive, currentPartIndex, currentWordCount))
                {
                    activeDirectives.Add(directive);
                }
            }

            return activeDirectives;
        }

        public string BuildPromptWithDirectives(string basePrompt, List<Directive> activeDirectives)
        {
            if (activeDirectives == null || !activeDirectives.Any())
                return basePrompt;

            var directiveTexts = activeDirectives.Select(d => d.DirectiveText);
            var combinedDirectives = string.Join("; ", directiveTexts);

            return $"[Story Direction: {combinedDirectives}]\n\n{basePrompt}";
        }

        public bool ShouldEnforceDirective(Directive directive, int currentPartIndex, int currentWordCount)
        {
            var trigger = directive.Trigger;

            // Check part-based trigger
            if (trigger.AtPart.HasValue)
            {
                if (directive.Strict)
                {
                    // For strict directives, enforce from the trigger point onwards until satisfied
                    return currentPartIndex >= trigger.AtPart.Value;
                }
                else
                {
                    // For non-strict directives, only trigger at the exact part
                    return currentPartIndex == trigger.AtPart.Value;
                }
            }

            // Check word count-based trigger
            if (trigger.AtWordCount.HasValue)
            {
                if (directive.Strict)
                {
                    // For strict directives, enforce from the trigger point onwards
                    return currentWordCount >= trigger.AtWordCount.Value;
                }
                else
                {
                    // For non-strict directives, trigger when we're within a reasonable range
                    var tolerance = 100; // words
                    return Math.Abs(currentWordCount - trigger.AtWordCount.Value) <= tolerance;
                }
            }

            return false;
        }

        public Directive? GetNextDirective(List<Directive> allDirectives, int currentPartIndex, int currentWordCount)
        {
            return allDirectives
                .Where(d => !ShouldEnforceDirective(d, currentPartIndex, currentWordCount))
                .Where(d => d.Trigger.AtPart > currentPartIndex || d.Trigger.AtWordCount > currentWordCount)
                .OrderBy(d => d.Trigger.AtPart ?? int.MaxValue)
                .ThenBy(d => d.Trigger.AtWordCount ?? int.MaxValue)
                .FirstOrDefault();
        }
    }
}