namespace AzureTtsBatchStudio.Tts
{
    public sealed class TtsPart
    {
        public int Index { get; init; }
        public string PlainText { get; init; } = "";
        public string SafeSsml { get; init; } = ""; // wrapped in <speak><voice>… with prosody user settings
        public int EstChars => SafeSsml.Length;
        public string OutputPath { get; set; } = "";
    }
}