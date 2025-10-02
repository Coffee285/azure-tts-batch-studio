using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    /// <summary>
    /// Service for validating SSML and checking compatibility with selected voice
    /// </summary>
    public interface ISsmlValidationService
    {
        ValidationResult ValidateSsml(string ssml, VoiceInfo voice);
        ValidationResult ValidateBatchRequest(string inputText, VoiceInfo voice, string outputDirectory);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public string GetSummary()
        {
            var sb = new StringBuilder();
            
            if (Errors.Any())
            {
                sb.AppendLine($"❌ {Errors.Count} Error(s):");
                foreach (var error in Errors)
                {
                    sb.AppendLine($"  • {error}");
                }
            }

            if (Warnings.Any())
            {
                sb.AppendLine($"⚠️  {Warnings.Count} Warning(s):");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  • {warning}");
                }
            }

            if (!Errors.Any() && !Warnings.Any())
            {
                sb.AppendLine("✅ Validation passed successfully");
            }

            return sb.ToString();
        }
    }

    public class SsmlValidationService : ISsmlValidationService
    {
        private static readonly HashSet<string> SupportedSsmlTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "speak", "voice", "prosody", "break", "emphasis", "sub", "phoneme",
            "say-as", "audio", "bookmark", "lang", "lexicon", "p", "s",
            "mstts:express-as", "mstts:silence", "mstts:viseme", "mstts:backgroundaudio"
        };

        /// <summary>
        /// Validates SSML markup and checks compatibility with the selected voice
        /// </summary>
        public ValidationResult ValidateSsml(string ssml, VoiceInfo voice)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(ssml))
            {
                result.IsValid = false;
                result.Errors.Add("SSML content is empty");
                return result;
            }

            // Try to parse as XML
            XDocument doc;
            try
            {
                doc = XDocument.Parse(ssml);
            }
            catch (System.Xml.XmlException ex)
            {
                result.IsValid = false;
                result.Errors.Add($"SSML is not well-formed XML: {ex.Message}");
                return result;
            }

            // Check root element
            if (doc.Root == null || doc.Root.Name.LocalName.ToLowerInvariant() != "speak")
            {
                result.IsValid = false;
                result.Errors.Add("SSML must have <speak> as the root element");
                return result;
            }

            // Validate all elements
            ValidateElements(doc.Root, voice, result);

            // Check for voice compatibility
            CheckVoiceCompatibility(doc, voice, result);

            return result;
        }

        /// <summary>
        /// Validates a batch request including text, voice selection, and output settings
        /// </summary>
        public ValidationResult ValidateBatchRequest(string inputText, VoiceInfo voice, string outputDirectory)
        {
            var result = new ValidationResult { IsValid = true };

            // Check input text
            if (string.IsNullOrWhiteSpace(inputText))
            {
                result.IsValid = false;
                result.Errors.Add("Input text is required");
            }
            else if (inputText.Length > 1000000) // 1MB limit
            {
                result.IsValid = false;
                result.Errors.Add("Input text exceeds maximum length (1MB)");
            }

            // Check voice selection
            if (voice == null)
            {
                result.IsValid = false;
                result.Errors.Add("Voice selection is required");
            }
            else if (string.IsNullOrEmpty(voice.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Selected voice is invalid");
            }

            // Check output directory
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                result.IsValid = false;
                result.Errors.Add("Output directory is required");
            }
            else if (!System.IO.Directory.Exists(outputDirectory))
            {
                result.IsValid = false;
                result.Errors.Add($"Output directory does not exist: {outputDirectory}");
            }

            // If text looks like SSML, validate it
            if (inputText.Trim().StartsWith("<speak", StringComparison.OrdinalIgnoreCase))
            {
                var ssmlResult = ValidateSsml(inputText, voice);
                result.Errors.AddRange(ssmlResult.Errors);
                result.Warnings.AddRange(ssmlResult.Warnings);
                result.IsValid = result.IsValid && ssmlResult.IsValid;
            }

            return result;
        }

        private void ValidateElements(XElement element, VoiceInfo voice, ValidationResult result)
        {
            // Check if tag is supported
            var tagName = element.Name.LocalName;
            if (!SupportedSsmlTags.Contains(tagName))
            {
                result.Warnings.Add($"Tag <{tagName}> may not be supported by Azure TTS");
            }

            // Validate prosody tags
            if (tagName.Equals("prosody", StringComparison.OrdinalIgnoreCase))
            {
                ValidateProsodyTag(element, voice, result);
            }

            // Validate style tags
            if (tagName.Equals("express-as", StringComparison.OrdinalIgnoreCase))
            {
                ValidateStyleTag(element, voice, result);
            }

            // Validate break tags
            if (tagName.Equals("break", StringComparison.OrdinalIgnoreCase))
            {
                ValidateBreakTag(element, result);
            }

            // Recursively validate child elements
            foreach (var child in element.Elements())
            {
                ValidateElements(child, voice, result);
            }
        }

        private void ValidateProsodyTag(XElement element, VoiceInfo voice, ValidationResult result)
        {
            // Check if voice supports prosody
            if (!voice.SupportsSpeakingRate && element.Attribute("rate") != null)
            {
                result.Warnings.Add($"Voice '{voice.DisplayName}' does not support rate adjustments. The rate attribute will be ignored.");
            }

            if (!voice.SupportsPitch && element.Attribute("pitch") != null)
            {
                result.Warnings.Add($"Voice '{voice.DisplayName}' does not support pitch adjustments. The pitch attribute will be ignored.");
            }

            // Validate attribute values
            var rateAttr = element.Attribute("rate");
            if (rateAttr != null)
            {
                var rateValue = rateAttr.Value.ToLowerInvariant();
                var validRates = new[] { "x-slow", "slow", "medium", "fast", "x-fast", "default" };
                
                // Check if it's a percentage or named value
                if (!validRates.Contains(rateValue) && !rateValue.EndsWith("%"))
                {
                    result.Warnings.Add($"Invalid rate value '{rateValue}'. Use x-slow, slow, medium, fast, x-fast, or a percentage.");
                }
            }

            var pitchAttr = element.Attribute("pitch");
            if (pitchAttr != null)
            {
                var pitchValue = pitchAttr.Value.ToLowerInvariant();
                var validPitches = new[] { "x-low", "low", "medium", "high", "x-high", "default" };
                
                if (!validPitches.Contains(pitchValue) && !pitchValue.EndsWith("%") && !pitchValue.EndsWith("hz"))
                {
                    result.Warnings.Add($"Invalid pitch value '{pitchValue}'. Use x-low, low, medium, high, x-high, a percentage, or Hz value.");
                }
            }
        }

        private void ValidateStyleTag(XElement element, VoiceInfo voice, ValidationResult result)
        {
            if (!voice.SupportsStyle)
            {
                result.Warnings.Add($"Voice '{voice.DisplayName}' does not support style expressions. Style tags will be ignored.");
                return;
            }

            var styleAttr = element.Attribute("style");
            if (styleAttr != null && voice.AvailableStyles.Any())
            {
                var styleValue = styleAttr.Value.ToLowerInvariant();
                if (!voice.AvailableStyles.Any(s => s.Equals(styleValue, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Warnings.Add($"Style '{styleValue}' may not be supported by voice '{voice.DisplayName}'. Available styles: {string.Join(", ", voice.AvailableStyles)}");
                }
            }

            var degreeAttr = element.Attribute("styledegree");
            if (degreeAttr != null)
            {
                if (!voice.SupportsStyleDegree)
                {
                    result.Warnings.Add($"Voice '{voice.DisplayName}' does not support style degree. The styledegree attribute will be ignored.");
                }
                else
                {
                    // Validate degree value (should be between 0.01 and 2)
                    if (double.TryParse(degreeAttr.Value, out var degree))
                    {
                        if (degree < 0.01 || degree > 2.0)
                        {
                            result.Warnings.Add($"Style degree '{degree}' is outside valid range (0.01 to 2.0)");
                        }
                    }
                }
            }
        }

        private void ValidateBreakTag(XElement element, ValidationResult result)
        {
            var timeAttr = element.Attribute("time");
            var strengthAttr = element.Attribute("strength");

            if (timeAttr != null)
            {
                var timeValue = timeAttr.Value.ToLowerInvariant();
                if (!timeValue.EndsWith("ms") && !timeValue.EndsWith("s"))
                {
                    result.Warnings.Add($"Break time '{timeValue}' should end with 'ms' or 's'");
                }
                else
                {
                    // Try to parse the numeric part
                    var numericPart = timeValue.TrimEnd('m', 's');
                    if (!int.TryParse(numericPart, out var time) || time < 0)
                    {
                        result.Warnings.Add($"Invalid break time value '{timeValue}'");
                    }
                }
            }

            if (strengthAttr != null)
            {
                var strength = strengthAttr.Value.ToLowerInvariant();
                var validStrengths = new[] { "none", "x-weak", "weak", "medium", "strong", "x-strong" };
                if (!validStrengths.Contains(strength))
                {
                    result.Warnings.Add($"Invalid break strength '{strength}'. Valid values: none, x-weak, weak, medium, strong, x-strong");
                }
            }
        }

        private void CheckVoiceCompatibility(XDocument doc, VoiceInfo voice, ValidationResult result)
        {
            // Check for voice elements
            var voiceElements = doc.Descendants().Where(e => e.Name.LocalName.Equals("voice", StringComparison.OrdinalIgnoreCase));
            foreach (var voiceElement in voiceElements)
            {
                var nameAttr = voiceElement.Attribute("name");
                if (nameAttr != null && !nameAttr.Value.Equals(voice.Name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Warnings.Add($"SSML specifies voice '{nameAttr.Value}' but selected voice is '{voice.Name}'. Selected voice will be used.");
                }
            }
        }
    }
}
