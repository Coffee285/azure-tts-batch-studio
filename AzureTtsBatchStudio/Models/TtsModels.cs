using System.ComponentModel;

namespace AzureTtsBatchStudio.Models
{
    public class VoiceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Locale { get; set; } = string.Empty;
        public string VoiceType { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{DisplayName} ({Gender})";
        }
    }

    public class LanguageInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class AudioFormat
    {
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;

        public override string ToString()
        {
            return Name;
        }
    }

    public class QualityOption
    {
        public string Name { get; set; } = string.Empty;
        public int BitRate { get; set; }
        public int SampleRate { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class TtsRequest
    {
        public string Text { get; set; } = string.Empty;
        public string OutputFileName { get; set; } = string.Empty;
        public VoiceInfo Voice { get; set; } = new();
        public double SpeakingRate { get; set; } = 1.0;
        public double Pitch { get; set; } = 0.0;
        public AudioFormat Format { get; set; } = new();
        public QualityOption Quality { get; set; } = new();
    }

    public class ProcessingProgress
    {
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public string CurrentItem { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}