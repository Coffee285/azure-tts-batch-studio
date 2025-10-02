using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using AzureTtsBatchStudio.Infrastructure.Common;

namespace AzureTtsBatchStudio.Features.StoryBuilderV2.Services
{
    /// <summary>
    /// Simple template engine for prompt templates with variable substitution
    /// </summary>
    public interface ITemplateEngine
    {
        /// <summary>
        /// Render a template with the provided variables
        /// </summary>
        string Render(string template, Dictionary<string, object> variables);
    }

    /// <summary>
    /// Simple template engine implementation
    /// Supports ${variable}, ${object.property}, and ${array[0]} syntax
    /// </summary>
    public class TemplateEngine : ITemplateEngine
    {
        private static readonly Regex VariablePattern = new Regex(
            @"\$\{([a-zA-Z0-9_.[\]]+)\}",
            RegexOptions.Compiled);

        public string Render(string template, Dictionary<string, object> variables)
        {
            Guard.AgainstNullOrWhiteSpace(template, nameof(template));
            Guard.AgainstNull(variables, nameof(variables));

            return VariablePattern.Replace(template, match =>
            {
                var variablePath = match.Groups[1].Value;
                var value = ResolveVariable(variablePath, variables);
                return value?.ToString() ?? match.Value;
            });
        }

        private object? ResolveVariable(string path, Dictionary<string, object> variables)
        {
            var parts = SplitPath(path);
            
            if (parts.Length == 0)
                return null;

            // Get root variable
            if (!variables.TryGetValue(parts[0], out var current))
                return null;

            // Navigate nested properties
            for (int i = 1; i < parts.Length; i++)
            {
                current = NavigateProperty(current, parts[i]);
                if (current == null)
                    return null;
            }

            return current;
        }

        private string[] SplitPath(string path)
        {
            // Handle both dot notation and bracket notation
            // e.g., "style.NarrativeVoice" or "characters[0].Name"
            var normalized = path
                .Replace("[", ".")
                .Replace("]", "");
            
            return normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        }

        private object? NavigateProperty(object obj, string propertyName)
        {
            if (obj == null)
                return null;

            // Handle dictionary
            if (obj is IDictionary<string, object> dict)
            {
                return dict.TryGetValue(propertyName, out var value) ? value : null;
            }

            // Handle list/array by index
            if (int.TryParse(propertyName, out var index))
            {
                if (obj is System.Collections.IList list && index >= 0 && index < list.Count)
                {
                    return list[index];
                }
            }

            // Handle object properties via reflection
            var type = obj.GetType();
            var property = type.GetProperty(propertyName);
            if (property != null)
            {
                return property.GetValue(obj);
            }

            return null;
        }
    }

    /// <summary>
    /// Template manager for loading and managing prompt templates
    /// </summary>
    public interface ITemplateManager
    {
        /// <summary>
        /// Get a template by name
        /// </summary>
        string GetTemplate(string name);

        /// <summary>
        /// Save a template (user-editable version)
        /// </summary>
        void SaveTemplate(string name, string content);

        /// <summary>
        /// Check if a user-editable version exists
        /// </summary>
        bool HasUserTemplate(string name);

        /// <summary>
        /// Reset to default template
        /// </summary>
        void ResetToDefault(string name);
    }

    /// <summary>
    /// Template manager implementation with embedded defaults and user overrides
    /// </summary>
    public class TemplateManager : ITemplateManager
    {
        private readonly string _userTemplatesPath;
        private readonly Dictionary<string, string> _defaultTemplates;

        public TemplateManager()
        {
            _userTemplatesPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AzureTtsBatchStudio",
                "templates");

            System.IO.Directory.CreateDirectory(_userTemplatesPath);

            _defaultTemplates = LoadDefaultTemplates();
        }

        public string GetTemplate(string name)
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));

            // Check for user override first
            var userPath = System.IO.Path.Combine(_userTemplatesPath, $"{name}.tmpl");
            if (System.IO.File.Exists(userPath))
            {
                return System.IO.File.ReadAllText(userPath);
            }

            // Fall back to default
            if (_defaultTemplates.TryGetValue(name, out var template))
            {
                return template;
            }

            throw new InvalidOperationException($"Template '{name}' not found");
        }

        public void SaveTemplate(string name, string content)
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));
            Guard.AgainstNull(content, nameof(content));

            var userPath = System.IO.Path.Combine(_userTemplatesPath, $"{name}.tmpl");
            System.IO.File.WriteAllText(userPath, content);
        }

        public bool HasUserTemplate(string name)
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));

            var userPath = System.IO.Path.Combine(_userTemplatesPath, $"{name}.tmpl");
            return System.IO.File.Exists(userPath);
        }

        public void ResetToDefault(string name)
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));

            var userPath = System.IO.Path.Combine(_userTemplatesPath, $"{name}.tmpl");
            if (System.IO.File.Exists(userPath))
            {
                System.IO.File.Delete(userPath);
            }
        }

        private Dictionary<string, string> LoadDefaultTemplates()
        {
            return new Dictionary<string, string>
            {
                ["Outline"] = GetOutlineTemplate(),
                ["BeatDraft"] = GetBeatDraftTemplate(),
                ["Refine"] = GetRefineTemplate(),
                ["SSMLify"] = GetSSMLifyTemplate()
            };
        }

        private string GetOutlineTemplate()
        {
            return @"You are a master horror story writer creating a compelling outline.

Story Title: ${title}
Genre: ${genre}
Target Duration: ${duration.MinMinutes}-${duration.MaxMinutes} minutes
Target WPM: ${duration.TargetWpm}
Narrative Voice: ${style.NarrativeVoice}
Tone: ${style.ToneAnchors}
Content Constraints: ${style.ContentConstraints}
Tropes to Avoid: ${style.TropesToAvoid}

Create a detailed 15-20 beat outline for a horror story that:
1. Follows a classic horror story arc (slow burn → midpoint shock → spiral → climax → denouement)
2. Includes specific sensory details and SFX cues for each beat
3. Maintains pacing appropriate for the target duration
4. Builds tension gradually with clear reversals and reveals
5. Avoids clichés and overused tropes
6. Respects the content constraints

For each beat, provide:
- Beat number and title
- 2-3 sentence summary
- Key mood/tone
- At least one SFX suggestion (e.g., [SFX:wind_low], [SFX:door_creak])
- Estimated word count (to fit target duration)

Output in Markdown format with clear beat divisions.";
        }

        private string GetBeatDraftTemplate()
        {
            return @"You are a master horror story writer expanding a beat into full prose.

Story Context:
Title: ${title}
Narrative Voice: ${style.NarrativeVoice}
Tone: ${style.ToneAnchors}
Content Constraints: ${style.ContentConstraints}

Characters:
${characters}

Beat to Expand:
${beat.Title}

Prompt:
${beat.Prompt}

Instructions:
1. Write 600-1200 words of tight, atmospheric prose
2. Use the narrative voice specified above
3. Include vivid sensory details (sight, sound, smell, touch)
4. Maintain tension throughout
5. End on a micro-cliffhanger or moment of unease
6. Insert [SFX:key=...] markers for sound effects (e.g., [SFX:key=door_creak])
7. Avoid graphic violence, hate content, and NSFW material
8. Use dialogue sparingly and purposefully

Output in Markdown format:
## ${beat.Title}

[Your prose here with [SFX:...] markers]";
        }

        private string GetRefineTemplate()
        {
            return @"You are a master editor refining horror story prose.

Original Draft:
${beat.DraftMd}

Story Context:
Style Guide: ${style.NarrativeVoice}
Tone: ${style.ToneAnchors}
Content Constraints: ${style.ContentConstraints}

Instructions:
1. Critique the draft for:
   - Clichés and overused horror tropes
   - Pacing issues (too fast or slow)
   - Clarity and readability
   - Continuity with characters and plot
   - Word count (target: 600-1200 words)
2. Rewrite the beat to improve:
   - Atmosphere and tension
   - Character voice consistency
   - Sensory details
   - Ending impact
3. Preserve [SFX:...] markers and add new ones if appropriate
4. Maintain the narrative voice

Output the refined version in Markdown format.";
        }

        private string GetSSMLifyTemplate()
        {
            return @"You are an expert at converting narrative prose into SSML for text-to-speech.

Prose:
${beat.DraftMd}

TTS Configuration:
Default Voice: ${tts.DefaultVoice}
Rate: ${tts.Rate}
Pitch: ${tts.Pitch}
Volume: ${tts.Volume}
Character Voice Map: ${tts.CharacterVoiceMap}

Instructions:
1. Convert the Markdown prose to SSML
2. Wrap the entire content in <speak> tags
3. Use <voice name=""...""> for character dialogue based on CharacterVoiceMap
4. Add <prosody rate=""..."" pitch=""..."" volume=""...""> based on TTS config
5. Use <break time=""...""/> for pauses (e.g., ""500ms"" for dramatic effect)
6. Use <emphasis level=""...""> for key words (level: reduced, moderate, strong)
7. Convert [SFX:key=...] markers to <audio src=""sfx/{key}.wav"" /> tags
8. Escape XML special characters (&, <, >, "", ')
9. Add <mstts:express-as style=""...""> for emotional tone if UseEmotion is true

Output valid SSML markup.";
        }
    }
}
