using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    /// <summary>
    /// Service for enhancing plain text into speech-optimized SSML using AI
    /// </summary>
    public interface ISsmlEnhancementService
    {
        Task<string> EnhanceTextForSpeechAsync(
            string inputText, 
            VoiceInfo voice,
            CancellationToken cancellationToken = default);
        bool IsConfigured { get; }
    }

    public class SsmlEnhancementService : ISsmlEnhancementService
    {
        private readonly IOpenAIClient _openAIClient;

        public bool IsConfigured => _openAIClient.IsConfigured;

        public SsmlEnhancementService(IOpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
        }

        /// <summary>
        /// Enhances plain text for speech using AI to generate SSML with appropriate
        /// pauses, emphasis, emotional tone, and natural speech rhythm
        /// </summary>
        public async Task<string> EnhanceTextForSpeechAsync(
            string inputText,
            VoiceInfo voice,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("OpenAI API key not configured. Please set the API key in Settings.");
            }

            if (string.IsNullOrWhiteSpace(inputText))
            {
                throw new ArgumentException("Input text cannot be empty", nameof(inputText));
            }

            // Build the system prompt based on voice capabilities
            var systemPrompt = BuildSystemPrompt(voice);

            // Create the request
            var request = new OpenAIGenerationRequest
            {
                Model = "gpt-4-turbo",  // Use GPT-4 Turbo for better SSML generation
                Messages = new System.Collections.Generic.List<OpenAIMessage>
                {
                    new OpenAIMessage 
                    { 
                        Role = "system", 
                        Content = systemPrompt 
                    },
                    new OpenAIMessage 
                    { 
                        Role = "user", 
                        Content = $"Transform the following text into engaging, human-sounding SSML suitable for text-to-speech:\n\n{inputText}" 
                    }
                },
                Temperature = 0.7,  // Balanced creativity
                MaxTokens = 4000,   // Allow for expansion
                TopP = 0.9
            };

            try
            {
                // Call OpenAI API
                var result = await _openAIClient.GenerateAsync(request, cancellationToken);

                if (!string.IsNullOrEmpty(result.Error))
                {
                    throw new InvalidOperationException($"OpenAI API error: {result.Error}");
                }

                // Extract and validate SSML
                var ssml = ExtractSsml(result.Text);
                ValidateSsml(ssml, voice);

                return ssml;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Failed to enhance text for speech: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Builds the system prompt based on voice capabilities
        /// </summary>
        private string BuildSystemPrompt(VoiceInfo voice)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are an expert in transforming text into SSML (Speech Synthesis Markup Language) for Azure Text-to-Speech.");
            prompt.AppendLine("Your goal is to make the text more engaging, natural, and human-sounding for speech synthesis.");
            prompt.AppendLine();
            prompt.AppendLine("Guidelines:");
            prompt.AppendLine("1. Return ONLY valid SSML markup wrapped in <speak> tags");
            prompt.AppendLine("2. Use appropriate <break> tags for natural pauses (e.g., <break time='500ms'/>)");
            prompt.AppendLine("3. Use <emphasis> tags to highlight important words or phrases");

            // Add voice-specific capabilities to the prompt
            if (voice.SupportsSpeakingRate && voice.SupportsPitch)
            {
                prompt.AppendLine("4. Use <prosody> tags with rate, pitch, and volume attributes for emotional expression");
            }
            else
            {
                prompt.AppendLine("4. DO NOT use <prosody> tags - this voice does not support rate/pitch adjustments");
            }

            if (voice.SupportsStyle)
            {
                prompt.AppendLine($"5. Use <mstts:express-as> with style attribute for emotional tone. Available styles: {string.Join(", ", voice.AvailableStyles)}");
            }

            if (voice.SupportsRole)
            {
                prompt.AppendLine($"6. Use role attribute for character voices. Available roles: {string.Join(", ", voice.AvailableRoles)}");
            }

            prompt.AppendLine();
            prompt.AppendLine("Format requirements:");
            prompt.AppendLine("- Include XML declaration and proper namespaces");
            prompt.AppendLine("- Use voice name: " + voice.Name);
            prompt.AppendLine("- Ensure all tags are properly closed");
            prompt.AppendLine("- Make the text flow naturally with appropriate pacing");
            prompt.AppendLine("- Add emotional depth and storytelling elements");
            prompt.AppendLine("- Return ONLY the SSML - no explanations or additional text");

            return prompt.ToString();
        }

        /// <summary>
        /// Extracts SSML from the AI response (handles cases where response includes explanation)
        /// </summary>
        private string ExtractSsml(string aiResponse)
        {
            var trimmed = aiResponse.Trim();
            
            // If it starts with <speak, assume it's pure SSML
            if (trimmed.StartsWith("<speak", StringComparison.OrdinalIgnoreCase))
            {
                // Find the end of the speak tag
                var endIndex = trimmed.LastIndexOf("</speak>", StringComparison.OrdinalIgnoreCase);
                if (endIndex > 0)
                {
                    return trimmed.Substring(0, endIndex + 8);  // +8 for "</speak>"
                }
                return trimmed;
            }

            // Try to find SSML within the response
            var startIndex = trimmed.IndexOf("<speak", StringComparison.OrdinalIgnoreCase);
            if (startIndex >= 0)
            {
                var endIndex = trimmed.LastIndexOf("</speak>", StringComparison.OrdinalIgnoreCase);
                if (endIndex > startIndex)
                {
                    return trimmed.Substring(startIndex, endIndex - startIndex + 8);
                }
            }

            // If no SSML found, throw error
            throw new InvalidOperationException("AI response did not contain valid SSML markup");
        }

        /// <summary>
        /// Validates SSML and checks for unsupported features
        /// </summary>
        private void ValidateSsml(string ssml, VoiceInfo voice)
        {
            try
            {
                // Parse XML to validate structure
                var doc = XDocument.Parse(ssml);

                // Check for unsupported features
                var warnings = new System.Collections.Generic.List<string>();

                // Check for prosody tags if not supported
                if (!voice.SupportsSpeakingRate || !voice.SupportsPitch)
                {
                    var prosodyTags = doc.Descendants().Where(e => e.Name.LocalName == "prosody");
                    if (prosodyTags.Any())
                    {
                        warnings.Add("Voice does not support prosody tags (rate/pitch). These will be ignored.");
                    }
                }

                // Check for style tags if not supported
                if (!voice.SupportsStyle)
                {
                    var styleTags = doc.Descendants().Where(e => e.Name.LocalName == "express-as");
                    if (styleTags.Any())
                    {
                        warnings.Add("Voice does not support style expressions. These will be ignored.");
                    }
                }

                // If there are warnings, we could log them, but for now we'll just validate structure
                // The actual rendering will ignore unsupported tags
            }
            catch (System.Xml.XmlException ex)
            {
                throw new InvalidOperationException($"Generated SSML is malformed: {ex.Message}", ex);
            }
        }
    }
}
