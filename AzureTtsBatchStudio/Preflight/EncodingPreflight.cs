using System;
using System.Collections.Generic;
using System.Text;
using UtfUnknown;

namespace AzureTtsBatchStudio.Preflight
{
    public sealed class EncodingPreflight
    {
        private static readonly Encoding Win1252;
        private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        static EncodingPreflight()
        {
            // Register code pages provider to support Windows-1252
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Win1252 = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }

        public PreflightResult Run(string input)
        {
            var result = new PreflightResult { Original = input };
            var beforeScore = MojibakeHeuristics.Score(input);
            result.ScoreBefore = beforeScore;

            // Always NFC normalize first (safe for diacritics).
            string nfc = input.Normalize(NormalizationForm.FormC);

            // 1) Try "1252 bytes -> UTF8 string" reinterpretation
            string fixed1252ToUtf8 = TryRecode(nfc, Win1252, Utf8);
            int score1252 = MojibakeHeuristics.Score(fixed1252ToUtf8);

            // 2) Try "UTF8 bytes -> 1252 string" reinterpretation
            string fixedUtf8To1252 = TryRecode(nfc, Utf8, Win1252);
            int scoreUtf8 = MojibakeHeuristics.Score(fixedUtf8To1252);

            // 3) Fallback small mapping table for the classic sequences if needed
            string mapped = MapClassicArtifacts(nfc, out var mapCounts);
            int scoreMapped = MojibakeHeuristics.Score(mapped);

            // Choose the lowest score
            (string text, string strategy, int afterScore) best = (nfc, "NormalizeOnly", MojibakeHeuristics.Score(nfc));
            if (score1252 < best.afterScore) best = (fixed1252ToUtf8, "1252->UTF8", score1252);
            if (scoreUtf8 < best.afterScore) best = (fixedUtf8To1252, "UTF8->1252", scoreUtf8);
            if (scoreMapped < best.afterScore) best = (mapped, "MapTable", scoreMapped);

            // Clean illegal XML chars (keep CR/LF/TAB).
            var cleaned = StripInvalidXml(best.text);

            result.Fixed = cleaned;
            result.ScoreAfter = MojibakeHeuristics.Score(cleaned);
            result.Strategy = best.strategy;
            foreach (var kv in mapCounts) result.Replacements[kv.Key] = kv.Value;

            // Non-fatal warnings
            if (result.ScoreAfter > 0 && result.ScoreAfter < result.ScoreBefore)
                result.Warnings.Add("Residual suspicious sequences remain after best fix; review diff.");

            return result;
        }

        private static string TryRecode(string s, Encoding assume, Encoding target)
        {
            try
            {
                // Interpret current chars as bytes of "assume", then decode those bytes as "target".
                var bytes = assume.GetBytes(s);
                return target.GetString(bytes);
            }
            catch { return s; }
        }

        private static string StripInvalidXml(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch == '\t' || ch == '\n' || ch == '\r' || !char.IsControl(ch))
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        private static readonly (string bad, string good)[] Map = new[]
        {
            ("\u00E2\u0080\u0099","'"),("\u00E2\u0080\u009C","\u201C"),("\u00E2\u0080\u009D","\u201D"),("\u00E2\u0080\u0093","\u2013"),("\u00E2\u0080\u0094","\u2014"),
            ("\u00E2\u0080\u0098","'"),("\u00E2\u0080\u00A2","\u2022"),("\u00E2\u0080\u00A6","\u2026"),
            ("\u00C3\u00A9","é"),("\u00C3\u00A1","á"),("\u00C3\u00B3","ó"),("\u00C3\u00B1","ñ")
            // Extend as needed
        };

        private static string MapClassicArtifacts(string s, out Dictionary<string, int> counts)
        {
            counts = new();
            var r = s;
            foreach (var (bad, good) in Map)
            {
                int before = MojibakeHeuristics.Score(r);
                r = r.Replace(bad, good, StringComparison.Ordinal);
                int after = MojibakeHeuristics.Score(r);
                if (after < before) counts[bad] = before - after;
            }
            return r;
        }
    }
}