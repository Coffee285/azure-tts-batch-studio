using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using AzureTtsBatchStudio.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureTtsBatchStudio.Services
{
    public interface IAzureTtsService
    {
        Task<List<Models.VoiceInfo>> GetAvailableVoicesAsync(string? locale = null);
        Task<List<LanguageInfo>> GetAvailableLanguagesAsync();
        Task<bool> TestConnectionAsync(string subscriptionKey, string region);
        Task<bool> GenerateSpeechAsync(TtsRequest request, string subscriptionKey, string region, CancellationToken cancellationToken = default);
        Task<bool> GenerateBatchSpeechAsync(List<TtsRequest> requests, string subscriptionKey, string region, 
            IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default);
        void ConfigureConnection(string subscriptionKey, string region);
        bool IsConfigured { get; }
    }

    public class AzureTtsService : IAzureTtsService
    {
        private string? _subscriptionKey;
        private string? _region;
        private SpeechConfig? _speechConfig;

        public bool IsConfigured => !string.IsNullOrEmpty(_subscriptionKey) && !string.IsNullOrEmpty(_region);

        public void ConfigureConnection(string subscriptionKey, string region)
        {
            _subscriptionKey = subscriptionKey;
            _region = region;
            _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
        }

        public async Task<bool> TestConnectionAsync(string subscriptionKey, string region)
        {
            try
            {
                var config = SpeechConfig.FromSubscription(subscriptionKey, region);
                using var synthesizer = new SpeechSynthesizer(config, null);
                
                // Try to get voices as a connection test
                var voicesResult = await synthesizer.GetVoicesAsync();
                return voicesResult.Reason == ResultReason.VoicesListRetrieved;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Models.VoiceInfo>> GetAvailableVoicesAsync(string? locale = null)
        {
            if (_speechConfig == null)
                return new List<Models.VoiceInfo>();

            try
            {
                using var synthesizer = new SpeechSynthesizer(_speechConfig, null);
                var voicesResult = await synthesizer.GetVoicesAsync(locale);

                if (voicesResult.Reason == ResultReason.VoicesListRetrieved)
                {
                    return voicesResult.Voices.Select(voice => new Models.VoiceInfo
                    {
                        Name = voice.Name,
                        DisplayName = voice.LocalName,
                        Language = voice.Locale,
                        Gender = voice.Gender.ToString(),
                        Locale = voice.Locale,
                        VoiceType = voice.VoiceType.ToString()
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting voices: {ex.Message}");
            }

            return new List<Models.VoiceInfo>();
        }

        public async Task<List<LanguageInfo>> GetAvailableLanguagesAsync()
        {
            var voices = await GetAvailableVoicesAsync();
            var languages = voices
                .GroupBy(v => v.Locale)
                .Select(g => new LanguageInfo
                {
                    Code = g.Key,
                    Name = g.Key,
                    DisplayName = GetLanguageDisplayName(g.Key)
                })
                .OrderBy(l => l.DisplayName)
                .ToList();

            return languages;
        }

        public async Task<bool> GenerateSpeechAsync(TtsRequest request, string subscriptionKey, string region, CancellationToken cancellationToken = default)
        {
            try
            {
                var config = SpeechConfig.FromSubscription(subscriptionKey, region);
                config.SpeechSynthesisVoiceName = request.Voice.Name;
                
                // Set the appropriate output format based on the request
                SetSpeechSynthesisOutputFormat(config, request.Format, request.Quality);
                
                var ssml = GenerateSsml(request.Text, request.Voice.Name, request.SpeakingRate, request.Pitch);
                
                // Use the appropriate audio config method based on the format
                AudioConfig audioConfig;
                if (request.Format.Name.ToUpperInvariant() == "WAV")
                {
                    audioConfig = AudioConfig.FromWavFileOutput(request.OutputFileName);
                }
                else
                {
                    audioConfig = AudioConfig.FromDefaultSpeakerOutput();
                }
                
                using (audioConfig)
                {
                    using var synthesizer = new SpeechSynthesizer(config, audioConfig);
                    
                    SpeechSynthesisResult result = await synthesizer.SpeakSsmlAsync(ssml);
                    if (request.Format.Name.ToUpperInvariant() != "WAV")
                    {
                        // For MP3 and OGG, we need to get the audio data and write it to file
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted && result.AudioData.Length > 0)
                        {
                            await File.WriteAllBytesAsync(request.OutputFileName, result.AudioData);
                        }
                    }
                    
                    return result.Reason == ResultReason.SynthesizingAudioCompleted;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating speech: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> GenerateBatchSpeechAsync(List<TtsRequest> requests, string subscriptionKey, string region, 
            IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var totalRequests = requests.Count;
            var processedCount = 0;
            var hasErrors = false;

            foreach (var request in requests)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    progress?.Report(new ProcessingProgress
                    {
                        TotalItems = totalRequests,
                        ProcessedItems = processedCount,
                        CurrentItem = Path.GetFileName(request.OutputFileName),
                        Status = $"Processing {processedCount + 1} of {totalRequests}..."
                    });

                    var success = await GenerateSpeechAsync(request, subscriptionKey, region, cancellationToken);
                    
                    if (!success)
                    {
                        hasErrors = true;
                        progress?.Report(new ProcessingProgress
                        {
                            TotalItems = totalRequests,
                            ProcessedItems = processedCount,
                            CurrentItem = Path.GetFileName(request.OutputFileName),
                            Status = $"Error processing {Path.GetFileName(request.OutputFileName)}",
                            HasError = true,
                            ErrorMessage = $"Failed to generate speech for {Path.GetFileName(request.OutputFileName)}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    hasErrors = true;
                    progress?.Report(new ProcessingProgress
                    {
                        TotalItems = totalRequests,
                        ProcessedItems = processedCount,
                        CurrentItem = Path.GetFileName(request.OutputFileName),
                        Status = $"Error: {ex.Message}",
                        HasError = true,
                        ErrorMessage = ex.Message
                    });
                }

                processedCount++;
            }

            progress?.Report(new ProcessingProgress
            {
                TotalItems = totalRequests,
                ProcessedItems = processedCount,
                CurrentItem = "",
                Status = hasErrors ? "Completed with errors" : "Completed successfully",
                IsCompleted = true,
                HasError = hasErrors
            });

            return !hasErrors;
        }

        private static void SetSpeechSynthesisOutputFormat(SpeechConfig config, AudioFormat format, QualityOption quality)
        {
            switch (format.Name.ToUpperInvariant())
            {
                case "MP3":
                    if (quality.BitRate >= 320)
                        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio48Khz192KBitRateMonoMp3);
                    else if (quality.BitRate >= 128)
                        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio48Khz96KBitRateMonoMp3);
                    else
                        // 96k is the lowest available MP3 bit rate in the Azure Speech SDK.
                        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio48Khz96KBitRateMonoMp3);
                    break;
                case "OGG":
                    if (quality.SampleRate >= 48000)
                        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Ogg48Khz16BitMonoOpus);
                    else
                        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Ogg24Khz16BitMonoOpus);
                    break;
                case "WAV":
                default:
                    if (quality.SampleRate >= 48000)
                        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff48Khz16BitMonoPcm);
                    else if (quality.SampleRate >= 44100)
                        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
                    else
                        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
                    break;
            }
        }

        private static string GenerateSsml(string text, string voiceName, double rate, double pitch)
        {
            var rateString = rate switch
            {
                < 0.7 => "x-slow",
                < 0.9 => "slow",
                <= 1.1 => "medium",
                <= 1.3 => "fast",
                _ => "x-fast"
            };

            var pitchString = pitch == 0 ? "default" : $"{pitch:+0;-0}%";

            return $@"
<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis'>
    <voice name='{voiceName}'>
        <prosody rate='{rateString}' pitch='{pitchString}'>
            {System.Security.SecurityElement.Escape(text)}
        </prosody>
    </voice>
</speak>";
        }

        private static string GetLanguageDisplayName(string locale)
        {
            return locale switch
            {
                "en-US" => "English (United States)",
                "en-GB" => "English (United Kingdom)",
                "en-AU" => "English (Australia)",
                "en-CA" => "English (Canada)",
                "es-ES" => "Spanish (Spain)",
                "es-MX" => "Spanish (Mexico)",
                "fr-FR" => "French (France)",
                "fr-CA" => "French (Canada)",
                "de-DE" => "German (Germany)",
                "it-IT" => "Italian (Italy)",
                "pt-BR" => "Portuguese (Brazil)",
                "pt-PT" => "Portuguese (Portugal)",
                "ja-JP" => "Japanese (Japan)",
                "ko-KR" => "Korean (Korea)",
                "zh-CN" => "Chinese (Mandarin, Simplified)",
                "zh-HK" => "Chinese (Cantonese, Traditional)",
                "zh-TW" => "Chinese (Taiwanese Mandarin)",
                "ru-RU" => "Russian (Russia)",
                "ar-SA" => "Arabic (Saudi Arabia)",
                "hi-IN" => "Hindi (India)",
                "nl-NL" => "Dutch (Netherlands)",
                "sv-SE" => "Swedish (Sweden)",
                "da-DK" => "Danish (Denmark)",
                "no-NO" => "Norwegian (Norway)",
                "fi-FI" => "Finnish (Finland)",
                "pl-PL" => "Polish (Poland)",
                "tr-TR" => "Turkish (Turkey)",
                "cs-CZ" => "Czech (Czech Republic)",
                "hu-HU" => "Hungarian (Hungary)",
                "ro-RO" => "Romanian (Romania)",
                "sk-SK" => "Slovak (Slovakia)",
                "sl-SI" => "Slovenian (Slovenia)",
                "bg-BG" => "Bulgarian (Bulgaria)",
                "hr-HR" => "Croatian (Croatia)",
                "et-EE" => "Estonian (Estonia)",
                "lv-LV" => "Latvian (Latvia)",
                "lt-LT" => "Lithuanian (Lithuania)",
                "mt-MT" => "Maltese (Malta)",
                _ => locale
            };
        }
    }
}