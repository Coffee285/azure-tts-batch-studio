using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AzureTtsBatchStudio.Models;
using AzureTtsBatchStudio.Services;
using Microsoft.Extensions.Logging;

namespace AzureTtsBatchStudio.Tts
{
    internal class AdaptiveBudgetManager
    {
        private int _currentBudget;
        private readonly int _originalBudget;
        private readonly int _minBudget;

        public AdaptiveBudgetManager(int originalBudget, int minBudget)
        {
            _originalBudget = originalBudget;
            _currentBudget = originalBudget;
            _minBudget = Math.Max(minBudget, 500); // Absolute minimum
        }

        public int CurrentBudget => _currentBudget;
        public bool HasReducedBudget => _currentBudget < _originalBudget;

        public bool TryReduceBudget()
        {
            var newBudget = (int)(_currentBudget * 0.85); // Reduce by 15%
            if (newBudget < _minBudget)
            {
                return false; // Cannot reduce further
            }
            
            _currentBudget = newBudget;
            return true;
        }
    }

    public sealed class TtsOrchestrator
    {
        private readonly IAzureTtsService _ttsService;
        private readonly FfmpegMerger _merger;
        private readonly ILogger? _logger;

        public event Action<string>? StatusChanged;
        public event Action<int, int>? ProgressChanged; // current, total

        public TtsOrchestrator(IAzureTtsService ttsService, ILogger? logger = null)
        {
            _ttsService = ttsService;
            _merger = new FfmpegMerger();
            _logger = logger;
        }

        public async Task<string> ProcessTextWithAdaptiveBudgetAsync(
            string inputText,
            TtsRenderOptions options,
            TtsRequest baseRequest,
            string subscriptionKey,
            string region,
            CancellationToken cancellationToken = default)
        {
            var budgetManager = new AdaptiveBudgetManager(
                options.TargetChunkChars - options.SafetyMarginChars,
                options.MinChunkChars);

            var currentOptions = options;
            
            while (true)
            {
                try
                {
                    return await ProcessTextAsync(inputText, currentOptions, baseRequest, subscriptionKey, region, cancellationToken);
                }
                catch (TtsException ttsEx) when (ttsEx.ErrorType == TtsErrorType.PayloadTooLarge)
                {
                    _logger?.LogWarning("Payload too large error occurred, attempting budget reduction");
                    
                    if (budgetManager.TryReduceBudget())
                    {
                        _logger?.LogInformation("Reducing budget from {Old} to {New} and retrying", 
                                               currentOptions.TargetChunkChars - currentOptions.SafetyMarginChars, 
                                               budgetManager.CurrentBudget);
                        
                        StatusChanged?.Invoke($"Content too large, reducing chunk size to {budgetManager.CurrentBudget} chars and retrying...");
                        
                        // Create new options with reduced budget
                        currentOptions = new TtsRenderOptions
                        {
                            MergeMode = options.MergeMode,
                            TargetChunkChars = budgetManager.CurrentBudget + options.SafetyMarginChars,
                            MinChunkChars = options.MinChunkChars,
                            SafetyMarginChars = options.SafetyMarginChars,
                            RespectSentenceBoundaries = options.RespectSentenceBoundaries,
                            KeepShortParagraphsTogether = options.KeepShortParagraphsTogether
                        };
                        
                        // Continue the loop to retry with new budget
                    }
                    else
                    {
                        // Cannot reduce budget further
                        throw new InvalidOperationException(
                            $"Cannot reduce chunk size further (current: {budgetManager.CurrentBudget}, minimum: {options.MinChunkChars}). " +
                            "The text may contain sentences that are too long to process individually.", ttsEx);
                    }
                }
            }
        }

        public async Task<string> ProcessTextAsync(
            string inputText,
            TtsRenderOptions options,
            TtsRequest baseRequest,
            string subscriptionKey,
            string region,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Starting TTS processing for text of {Length} characters", inputText.Length);
            
            StatusChanged?.Invoke("Analyzing and chunking text...");
            
            // Determine if input is SSML or plain text
            bool isSSML = DetectSSML(inputText);
            
            // Create chunker and split text
            var chunker = new TtsChunker(options);
            IReadOnlyList<TtsPart> parts;
            
            if (isSSML)
            {
                _logger?.LogInformation("Processing as SSML input");
                var ssmlDoc = XDocument.Parse(inputText);
                parts = chunker.SplitSsml(ssmlDoc, text => GenerateSSML(text, baseRequest));
            }
            else
            {
                _logger?.LogInformation("Processing as plain text input");
                parts = chunker.SplitPlainText(inputText, text => GenerateSSML(text, baseRequest));
            }

            _logger?.LogInformation("Split text into {Count} parts", parts.Count);
            StatusChanged?.Invoke($"Split into {parts.Count} parts. Starting rendering...");

            // Create output directory
            var outputDir = Path.GetDirectoryName(baseRequest.OutputFileName) ?? 
                           throw new ArgumentException("Invalid output file path");
            Directory.CreateDirectory(outputDir);

            // Render each part
            var successfulParts = new List<TtsPart>();
            var failedParts = new List<(TtsPart Part, Exception Error)>();

            for (int i = 0; i < parts.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var part = parts[i];
                ProgressChanged?.Invoke(i + 1, parts.Count);
                StatusChanged?.Invoke($"Rendering part {i + 1} of {parts.Count} ({part.EstChars} chars)...");

                try
                {
                    var partRequest = CreatePartRequest(baseRequest, part, outputDir);
                    
                    bool success = await RenderPartWithRetry(partRequest, subscriptionKey, region, cancellationToken);
                    
                    if (success)
                    {
                        part.OutputPath = partRequest.OutputFileName;
                        successfulParts.Add(part);
                        _logger?.LogInformation("Successfully rendered part {Index}: {Path}", part.Index, part.OutputPath);
                    }
                    else
                    {
                        var error = new Exception($"Failed to render part {part.Index} after retries");
                        failedParts.Add((part, error));
                        _logger?.LogError("Failed to render part {Index} after retries", part.Index);
                    }
                }
                catch (Exception ex)
                {
                    failedParts.Add((part, ex));
                    _logger?.LogError(ex, "Error rendering part {Index}", part.Index);
                }
            }

            if (successfulParts.Count == 0)
            {
                throw new InvalidOperationException("No parts were successfully rendered");
            }

            if (failedParts.Count > 0)
            {
                _logger?.LogWarning("Completed with {Failed} failed parts out of {Total}", 
                                   failedParts.Count, parts.Count);
            }

            // Merge if requested and multiple parts
            if (options.MergeMode == MergeMode.SingleMergedMp3 && successfulParts.Count > 1)
            {
                StatusChanged?.Invoke($"Merging {successfulParts.Count} parts...");
                
                var outputPath = baseRequest.OutputFileName;
                var partPaths = successfulParts.Select(p => p.OutputPath).ToList();
                
                try
                {
                    await _merger.MergeAsync(partPaths, outputPath, cancellationToken);
                    _logger?.LogInformation("Successfully merged {Count} parts to {Path}", partPaths.Count, outputPath);
                    
                    // Clean up individual part files
                    foreach (var partPath in partPaths)
                    {
                        try
                        {
                            File.Delete(partPath);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to delete part file {Path}", partPath);
                        }
                    }
                    
                    StatusChanged?.Invoke($"Completed! Merged {successfulParts.Count} parts into single file.");
                    return outputPath;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to merge parts");
                    StatusChanged?.Invoke($"Rendering completed but merge failed: {ex.Message}");
                    
                    // Return the directory containing part files
                    return outputDir;
                }
            }
            else
            {
                // Return single file or directory with separate files
                if (successfulParts.Count == 1)
                {
                    StatusChanged?.Invoke("Completed! Generated single audio file.");
                    return successfulParts[0].OutputPath;
                }
                else
                {
                    StatusChanged?.Invoke($"Completed! Generated {successfulParts.Count} separate audio files.");
                    return outputDir;
                }
            }
        }

        private static bool DetectSSML(string text)
        {
            var trimmed = text.Trim();
            return trimmed.StartsWith("<speak", StringComparison.OrdinalIgnoreCase) && 
                   trimmed.EndsWith("</speak>", StringComparison.OrdinalIgnoreCase);
        }

        private static string GenerateSSML(string text, TtsRequest baseRequest)
        {
            // For voices that don't support prosody, generate simple SSML
            if (!baseRequest.Voice.SupportsSpeakingRate && !baseRequest.Voice.SupportsPitch)
            {
                return $@"<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis'>
    <voice name='{baseRequest.Voice.Name}'>
        {System.Security.SecurityElement.Escape(text)}
    </voice>
</speak>";
            }
            
            var rateString = baseRequest.SpeakingRate switch
            {
                < 0.7 => "x-slow",
                < 0.9 => "slow",
                <= 1.1 => "medium",
                <= 1.3 => "fast",
                _ => "x-fast"
            };

            var pitchString = baseRequest.Pitch == 0 ? "default" : $"{baseRequest.Pitch:+0;-0}%";

            return $@"<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis'>
    <voice name='{baseRequest.Voice.Name}'>
        <prosody rate='{rateString}' pitch='{pitchString}'>
            {System.Security.SecurityElement.Escape(text)}
        </prosody>
    </voice>
</speak>";
        }

        private static TtsRequest CreatePartRequest(TtsRequest baseRequest, TtsPart part, string outputDir)
        {
            var extension = Path.GetExtension(baseRequest.OutputFileName);
            var baseFileName = Path.GetFileNameWithoutExtension(baseRequest.OutputFileName);
            var partFileName = $"{baseFileName}_part_{part.Index:D3}{extension}";
            var partPath = Path.Combine(outputDir, partFileName).Replace('\\', '/');

            return new TtsRequest
            {
                Text = part.SafeSsml,
                OutputFileName = partPath,
                Voice = baseRequest.Voice,
                SpeakingRate = baseRequest.SpeakingRate,
                Pitch = baseRequest.Pitch,
                Format = baseRequest.Format,
                Quality = baseRequest.Quality
            };
        }

        private async Task<bool> RenderPartWithRetry(TtsRequest request, string subscriptionKey, string region, CancellationToken cancellationToken)
        {
            const int maxAttempts = 3;
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    bool success = await _ttsService.GenerateSpeechAsync(request, subscriptionKey, region, cancellationToken);
                    if (success) return true;
                    
                    _logger?.LogWarning("Attempt {Attempt}/{Max} failed for part request", attempt, maxAttempts);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Attempt {Attempt}/{Max} failed with exception", attempt, maxAttempts);
                    
                    // If this looks like a 413 (too large), we could implement dynamic budget reduction here
                    // For now, just retry with exponential backoff
                }

                if (attempt < maxAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, cancellationToken);
                }
            }

            return false;
        }
    }
}