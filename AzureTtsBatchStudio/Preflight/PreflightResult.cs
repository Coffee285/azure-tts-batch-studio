using System.Collections.Generic;

namespace AzureTtsBatchStudio.Preflight
{
    public sealed class PreflightResult
    {
        public string Original { get; set; } = "";
        public string Fixed { get; set; } = "";
        public string Strategy { get; set; } = ""; // "None", "1252->UTF8", "UTF8->1252", "MapTable", "NormalizeOnly"
        public string? DetectedEncoding { get; set; }
        public int ScoreBefore { get; set; }  // mojibake score
        public int ScoreAfter { get; set; }
        public List<string> Warnings { get; } = new();
        public Dictionary<string, int> Replacements { get; } = new(); // "â€™"->42
    }
}