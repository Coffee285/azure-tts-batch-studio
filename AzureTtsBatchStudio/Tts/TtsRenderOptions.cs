namespace AzureTtsBatchStudio.Tts
{
    public enum MergeMode 
    { 
        SingleMergedMp3, 
        SeparateFilesOnly 
    }

    public sealed class TtsRenderOptions
    {
        public MergeMode MergeMode { get; init; } = MergeMode.SingleMergedMp3;
        public int TargetChunkChars { get; init; } = 2000;   // safe default
        public int MinChunkChars { get; init; } = 1400;      // avoid micro-chunks
        public int SafetyMarginChars { get; init; } = 200;   // buffer for SSML envelope
        public bool RespectSentenceBoundaries { get; init; } = true;
        public bool KeepShortParagraphsTogether { get; init; } = true;
    }
}