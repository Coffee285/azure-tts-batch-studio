using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AzureTtsBatchStudio.Tts
{
    public sealed class TtsChunker
    {
        private readonly TtsRenderOptions _opts;
        
        // Sentence splitting regex that respects punctuation and closing quotes/parentheses
        private static readonly Regex SentenceSplit = 
            new(@"(?<=[\.!\?…][\""\)\]]?\s+)|(?<=[\n\r]+)", RegexOptions.Compiled);

        public TtsChunker(TtsRenderOptions opts) => _opts = opts;

        public IReadOnlyList<TtsPart> SplitPlainText(string text, Func<string, string> ssmlWrap)
        {
            var parts = new List<TtsPart>();
            var normalizedText = text.Normalize(System.Text.NormalizationForm.FormC);
            var sentences = SentenceSplit.Split(normalizedText)
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .ToList();

            var sb = new StringBuilder();
            int idx = 1;
            int budget = _opts.TargetChunkChars - _opts.SafetyMarginChars;

            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();
                if (string.IsNullOrEmpty(trimmedSentence)) continue;

                // If this sentence alone exceeds budget, hard-wrap it
                if (trimmedSentence.Length > budget)
                {
                    // Emit any accumulated content first
                    if (sb.Length > 0)
                    {
                        Emit(ref parts, ref sb, ref idx, ssmlWrap);
                    }
                    
                    // Hard-wrap the long sentence
                    var wrappedParts = HardWrapLongSentence(trimmedSentence, budget);
                    foreach (var part in wrappedParts)
                    {
                        sb.Append(part);
                        Emit(ref parts, ref sb, ref idx, ssmlWrap);
                    }
                    continue;
                }

                // Check if adding this sentence would exceed budget
                if (sb.Length + trimmedSentence.Length > budget)
                {
                    Emit(ref parts, ref sb, ref idx, ssmlWrap);
                }

                if (sb.Length > 0 && !char.IsWhiteSpace(sb[sb.Length - 1]) && !char.IsWhiteSpace(trimmedSentence[0]))
                {
                    sb.Append(' ');
                }
                sb.Append(trimmedSentence);
            }

            // Emit any remaining content
            if (sb.Length > 0)
            {
                Emit(ref parts, ref sb, ref idx, ssmlWrap);
            }

            // Merge micro-chunks if needed
            return CoalesceSmall(parts, _opts.MinChunkChars).ToList();
        }

        public IReadOnlyList<TtsPart> SplitSsml(XDocument ssmlDoc, Func<string, string> ssmlWrap)
        {
            var parts = new List<TtsPart>();
            var textNodes = ExtractTextNodesInOrder(ssmlDoc);
            
            var sb = new StringBuilder();
            int idx = 1;
            int budget = _opts.TargetChunkChars - _opts.SafetyMarginChars;

            foreach (var textNode in textNodes)
            {
                var text = textNode.Trim();
                if (string.IsNullOrEmpty(text)) continue;

                if (sb.Length + text.Length > budget)
                {
                    if (sb.Length > 0)
                    {
                        EmitSsml(ref parts, ref sb, ref idx, ssmlWrap);
                    }
                }

                if (sb.Length > 0) sb.Append(' ');
                sb.Append(text);
            }

            if (sb.Length > 0)
            {
                EmitSsml(ref parts, ref sb, ref idx, ssmlWrap);
            }

            return CoalesceSmall(parts, _opts.MinChunkChars).ToList();
        }

        private static void Emit(ref List<TtsPart> parts, ref StringBuilder sb, ref int idx, Func<string, string> ssmlWrap)
        {
            var chunkText = sb.ToString().Trim();
            if (chunkText.Length == 0) return;
            
            parts.Add(new TtsPart 
            { 
                Index = idx++, 
                PlainText = chunkText, 
                SafeSsml = ssmlWrap(chunkText) 
            });
            sb.Clear();
        }

        private static void EmitSsml(ref List<TtsPart> parts, ref StringBuilder sb, ref int idx, Func<string, string> ssmlWrap)
        {
            var chunkText = sb.ToString().Trim();
            if (chunkText.Length == 0) return;

            var ssml = ssmlWrap(chunkText);
            
            // Validate SSML is well-formed
            try
            {
                XDocument.Parse(ssml);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Generated SSML chunk {idx} is not well-formed: {ex.Message}");
            }
            
            parts.Add(new TtsPart 
            { 
                Index = idx++, 
                PlainText = chunkText, 
                SafeSsml = ssml 
            });
            sb.Clear();
        }

        private static List<string> HardWrapLongSentence(string sentence, int budget)
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var parts = new List<string>();
            var sb = new StringBuilder();
            int wrapBudget = (int)(budget * 0.95); // Use 95% of budget for safety

            foreach (var word in words)
            {
                if (sb.Length + word.Length + 1 > wrapBudget)
                {
                    if (sb.Length > 0)
                    {
                        parts.Add(sb.ToString().Trim());
                        sb.Clear();
                    }
                }

                if (sb.Length > 0) sb.Append(' ');
                sb.Append(word);
            }

            if (sb.Length > 0)
            {
                parts.Add(sb.ToString().Trim());
            }

            return parts;
        }

        private static IEnumerable<string> ExtractTextNodesInOrder(XDocument doc)
        {
            // Walk all text nodes in document order
            return doc.Descendants()
                      .Where(e => !e.HasElements)
                      .Select(e => e.Value)
                      .Where(text => !string.IsNullOrWhiteSpace(text));
        }

        private static IEnumerable<TtsPart> CoalesceSmall(IEnumerable<TtsPart> parts, int minChars)
        {
            var result = new List<TtsPart>();
            TtsPart? accumulator = null;

            foreach (var part in parts)
            {
                if (part.PlainText.Length < minChars && accumulator != null)
                {
                    // Merge with accumulator
                    var merged = new TtsPart
                    {
                        Index = accumulator.Index,
                        PlainText = accumulator.PlainText + " " + part.PlainText,
                        SafeSsml = accumulator.SafeSsml + " " + part.SafeSsml,
                        OutputPath = accumulator.OutputPath
                    };
                    accumulator = merged;
                }
                else
                {
                    if (accumulator != null)
                    {
                        result.Add(accumulator);
                    }
                    accumulator = part;
                }
            }

            if (accumulator != null)
            {
                result.Add(accumulator);
            }

            return result;
        }
    }
}