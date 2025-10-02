using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    /// <summary>
    /// Service for fetching and caching Azure TTS voice capability metadata
    /// including style, role, and prosody support information
    /// </summary>
    public interface IVoiceCapabilityService
    {
        Task<VoiceCapabilities?> GetVoiceCapabilitiesAsync(string voiceName);
        Task EnrichVoiceInfoAsync(VoiceInfo voice);
    }

    public class VoiceCapabilities
    {
        public bool SupportsStyle { get; set; }
        public bool SupportsStyleDegree { get; set; }
        public bool SupportsRole { get; set; }
        public List<string> AvailableStyles { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
    }

    public class VoiceCapabilityService : IVoiceCapabilityService
    {
        private readonly Dictionary<string, VoiceCapabilities> _capabilitiesCache = new();
        
        /// <summary>
        /// Gets voice capabilities for a specific voice
        /// </summary>
        public async Task<VoiceCapabilities?> GetVoiceCapabilitiesAsync(string voiceName)
        {
            // Check cache first
            if (_capabilitiesCache.TryGetValue(voiceName, out var cached))
            {
                return cached;
            }

            // Determine capabilities based on voice name patterns
            // Azure Neural voices typically support styles and roles
            var capabilities = DetermineCapabilitiesFromVoiceName(voiceName);
            
            // Cache the result
            _capabilitiesCache[voiceName] = capabilities;
            
            return capabilities;
        }

        /// <summary>
        /// Enriches a VoiceInfo object with capability information
        /// </summary>
        public async Task EnrichVoiceInfoAsync(VoiceInfo voice)
        {
            var capabilities = await GetVoiceCapabilitiesAsync(voice.Name);
            if (capabilities != null)
            {
                voice.SupportsStyle = capabilities.SupportsStyle;
                voice.SupportsStyleDegree = capabilities.SupportsStyleDegree;
                voice.SupportsRole = capabilities.SupportsRole;
                voice.AvailableStyles = capabilities.AvailableStyles;
                voice.AvailableRoles = capabilities.AvailableRoles;
            }
        }

        /// <summary>
        /// Determines voice capabilities based on voice name patterns and known features
        /// </summary>
        private VoiceCapabilities DetermineCapabilitiesFromVoiceName(string voiceName)
        {
            var capabilities = new VoiceCapabilities();
            var lowerName = voiceName.ToLowerInvariant();

            // Azure Neural voices typically support style and role
            // Format: locale-Name-VoiceType (e.g., en-US-AriaNeural, zh-CN-XiaoxiaoNeural)
            bool isNeuralVoice = lowerName.Contains("neural");
            
            // Check for specific voices known to support styles
            // Based on Azure documentation: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/speech-synthesis-markup-voice
            var stylesupportedVoices = new[]
            {
                // English (US)
                "en-us-arianeural", "en-us-guyneural", "en-us-daveneural", "en-us-jennyneural",
                "en-us-janeneural", "en-us-jasonneural", "en-us-sarachneural", "en-us-tonyneural",
                // English (UK)
                "en-gb-sonianeural", "en-gb-ryanneural",
                // English (Australia)
                "en-au-natashanerural", "en-au-williamneural",
                // Chinese
                "zh-cn-xiaoxiaoneural", "zh-cn-yunxineural", "zh-cn-yunxianeural", "zh-cn-yunyang",
                "zh-cn-xiaochenneural", "zh-cn-xiaohaneural", "zh-cn-xiaomoaneural", "zh-cn-xiaoruineural",
                "zh-cn-xiaoxuanneural", "zh-cn-xiaoyaneural", "zh-cn-xiaoyineural", "zh-cn-xiaoyouneural",
                "zh-cn-yunxineural", "zh-cn-yunxianeural", "zh-cn-yunyang",
                // Japanese
                "ja-jp-nanamineural", "ja-jp-keitaneural",
                // Spanish
                "es-es-elviraneural", "es-mx-dalianeural",
                // French
                "fr-fr-deniseneural", "fr-fr-henrineural",
                // German
                "de-de-katjaneural", "de-de-conradneural",
                // Italian
                "it-it-elsaneural", "it-it-isabellaneural", "it-it-diegoneural",
                // Portuguese
                "pt-br-franciscaneural", "pt-br-antoniooneural"
            };

            bool supportsStyle = stylesupportedVoices.Any(v => lowerName.Contains(v));

            if (supportsStyle)
            {
                capabilities.SupportsStyle = true;
                capabilities.SupportsStyleDegree = true;  // Most voices that support style also support degree
                
                // Common styles across many voices
                capabilities.AvailableStyles = new List<string>
                {
                    "cheerful", "sad", "angry", "fearful", "neutral", 
                    "calm", "assistant", "chat", "customerservice", 
                    "newscast", "affectionate", "gentle", "lyrical",
                    "excited", "friendly", "hopeful", "shouting",
                    "terrified", "unfriendly", "whispering"
                };
            }

            // Check for role support (typically multilingual/story-telling voices)
            var roleSupported = new[]
            {
                "en-us-arianeural", "en-us-daveneural", "en-us-guyneural", "en-us-jennyneural",
                "zh-cn-xiaoxiaoneural", "zh-cn-yunxineural", "zh-cn-yunxianeural",
                "ja-jp-nanamineural"
            };

            if (roleSupported.Any(v => lowerName.Contains(v)))
            {
                capabilities.SupportsRole = true;
                capabilities.AvailableRoles = new List<string>
                {
                    "Girl", "Boy", "YoungAdultFemale", "YoungAdultMale",
                    "OlderAdultFemale", "OlderAdultMale", "SeniorFemale", "SeniorMale"
                };
            }

            return capabilities;
        }
    }
}
