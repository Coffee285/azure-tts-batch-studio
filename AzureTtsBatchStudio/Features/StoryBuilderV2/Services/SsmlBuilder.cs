using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using AzureTtsBatchStudio.Features.StoryBuilderV2.Models;
using AzureTtsBatchStudio.Infrastructure.Common;

namespace AzureTtsBatchStudio.Features.StoryBuilderV2.Services
{
    /// <summary>
    /// Service for building SSML from Markdown and story beats
    /// </summary>
    public interface ISsmlBuilder
    {
        /// <summary>
        /// Convert a beat's Markdown content to SSML
        /// </summary>
        string BuildSsml(StoryBeat beat, TtsProfile ttsProfile);

        /// <summary>
        /// Validate SSML markup
        /// </summary>
        Result ValidateSsml(string ssml);
    }

    /// <summary>
    /// SSML builder implementation
    /// </summary>
    public class SsmlBuilder : ISsmlBuilder
    {
        private static readonly Regex SfxPattern = new Regex(
            @"\[SFX:(?:key=)?([^\]]+)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MarkdownHeaderPattern = new Regex(
            @"^#{1,6}\s+(.+)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public string BuildSsml(StoryBeat beat, TtsProfile ttsProfile)
        {
            Guard.AgainstNull(beat, nameof(beat));
            Guard.AgainstNull(ttsProfile, nameof(ttsProfile));

            var content = beat.DraftMd;
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            // Remove Markdown headers
            content = MarkdownHeaderPattern.Replace(content, "$1");

            // Build SSML
            var ssml = new StringBuilder();
            ssml.AppendLine("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\"");
            ssml.AppendLine("        xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"en-US\">");

            // Add voice wrapper
            ssml.AppendLine($"  <voice name=\"{EscapeXml(ttsProfile.DefaultVoice)}\">");

            // Add prosody if non-default
            var needsProsody = Math.Abs(ttsProfile.Rate - 1.0) > 0.01 ||
                              Math.Abs(ttsProfile.Pitch) > 0.01 ||
                              Math.Abs(ttsProfile.Volume) > 0.01;

            if (needsProsody)
            {
                var rateStr = FormatRate(ttsProfile.Rate);
                var pitchStr = FormatPitch(ttsProfile.Pitch);
                var volumeStr = FormatVolume(ttsProfile.Volume);

                ssml.AppendLine($"    <prosody rate=\"{rateStr}\" pitch=\"{pitchStr}\" volume=\"{volumeStr}\">");
            }

            // Process content: replace SFX markers with audio tags (DON'T escape the result)
            var processedContent = ProcessContent(content, beat.Sfx);
            ssml.AppendLine($"      {processedContent}");

            if (needsProsody)
            {
                ssml.AppendLine("    </prosody>");
            }

            ssml.AppendLine("  </voice>");
            ssml.AppendLine("</speak>");

            return ssml.ToString();
        }

        public Result ValidateSsml(string ssml)
        {
            try
            {
                Guard.AgainstNullOrWhiteSpace(ssml, nameof(ssml));

                // Try to parse as XML
                var doc = XDocument.Parse(ssml);

                // Check for required elements
                var root = doc.Root;
                if (root == null || root.Name.LocalName != "speak")
                {
                    return Result.Failure("SSML must have a <speak> root element");
                }

                // Check for voice element
                var voiceElements = root.Descendants()
                    .Where(e => e.Name.LocalName == "voice")
                    .ToList();

                if (!voiceElements.Any())
                {
                    return Result.Failure("SSML must contain at least one <voice> element");
                }

                // Check for valid voice names
                foreach (var voice in voiceElements)
                {
                    var nameAttr = voice.Attribute("name");
                    if (nameAttr == null || string.IsNullOrWhiteSpace(nameAttr.Value))
                    {
                        return Result.Failure("Voice element must have a 'name' attribute");
                    }
                }

                return Result.Success();
            }
            catch (XmlException ex)
            {
                return Result.Failure($"Invalid XML: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result.Failure($"SSML validation error: {ex.Message}");
            }
        }

        private string ProcessContent(string content, List<InlineSfxCue> sfxCues)
        {
            // First escape XML special characters in the text
            var escaped = EscapeXml(content);
            
            // Then replace [SFX:key] markers with <audio> tags (not escaped)
            var result = SfxPattern.Replace(escaped, match =>
            {
                var key = match.Groups[1].Value.Trim();
                return $"<audio src=\"sfx/{key}.wav\" />";
            });

            return result;
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private static string FormatRate(double rate)
        {
            if (Math.Abs(rate - 1.0) < 0.01)
                return "1.0";

            return $"{rate:F1}";
        }

        private static string FormatPitch(double pitch)
        {
            if (Math.Abs(pitch) < 0.01)
                return "+0%";

            return pitch > 0 ? $"+{pitch:F0}%" : $"{pitch:F0}%";
        }

        private static string FormatVolume(double volume)
        {
            if (Math.Abs(volume) < 0.01)
                return "+0dB";

            return volume > 0 ? $"+{volume:F0}dB" : $"{volume:F0}dB";
        }
    }

    /// <summary>
    /// Service for estimating story duration from word count
    /// </summary>
    public interface IDurationEstimator
    {
        /// <summary>
        /// Estimate duration in minutes from word count
        /// </summary>
        double EstimateDurationMinutes(int wordCount, int wpm = 130);

        /// <summary>
        /// Count words in text
        /// </summary>
        int CountWords(string text);
    }

    /// <summary>
    /// Duration estimator implementation
    /// </summary>
    public class DurationEstimator : IDurationEstimator
    {
        public double EstimateDurationMinutes(int wordCount, int wpm = 130)
        {
            if (wordCount <= 0 || wpm <= 0)
                return 0;

            return (double)wordCount / wpm;
        }

        public int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // Remove Markdown formatting and SFX markers
            text = MarkdownHeaderPattern.Replace(text, "$1");
            text = SfxPattern.Replace(text, "");

            // Split by whitespace and count
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, 
                StringSplitOptions.RemoveEmptyEntries);

            return words.Length;
        }

        private static readonly Regex MarkdownHeaderPattern = new Regex(
            @"^#{1,6}\s+(.+)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex SfxPattern = new Regex(
            @"\[SFX:(?:key=)?([^\]]+)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
