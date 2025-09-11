using System;

namespace AzureTtsBatchStudio.Preflight
{
    public static class MojibakeHeuristics
    {
        // Count "suspect" patterns typical of wrongly decoded UTF-8 as 1252 and vice versa.
        private static readonly string[] Suspects = {
            "\u00E2\u0080\u0099","\u00E2\u0080\u009C","\u00E2\u0080\u009D","\u00E2\u0080\u0093","\u00E2\u0080\u0094","\u00E2\u0080\u0098","\u00E2\u0080\u00A2","\u00E2\u0080\u00A6",
            "\u00C3\u00A0","\u00C3\u00A1","\u00C3\u00A2","\u00C3\u00A3","\u00C3\u00A4","\u00C3\u00A5","\u00C3\u00A6","\u00C3\u00A7","\u00C3\u00A8","\u00C3\u00A9","\u00C3\u00AA","\u00C3\u00AB","\u00C3\u00AC","\u00C3\u008D","\u00C3\u00AE","\u00C3\u00AF",
            "\u00C2","\u20AC","\u2019","\u0153","\u017E","\u0178"
        };

        public static int Score(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int score = 0;
            foreach (var sig in Suspects) score += CountOf(s, sig);
            // Penalize U+FFFD and stray control chars (except \r \n \t)
            score += CountOf(s, "�");
            foreach (var ch in s)
                if (char.IsControl(ch) && ch is not ('\r' or '\n' or '\t')) score++;
            return score;
        }

        private static int CountOf(string s, string needle)
        {
            int count = 0, idx = 0;
            while ((idx = s.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0) { count++; idx += needle.Length; }
            return count;
        }
    }
}