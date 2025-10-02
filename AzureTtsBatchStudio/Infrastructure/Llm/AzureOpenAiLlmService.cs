using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AzureTtsBatchStudio.Infrastructure.Common;
using Polly;
using Polly.Retry;

namespace AzureTtsBatchStudio.Infrastructure.Llm
{
    /// <summary>
    /// Azure OpenAI implementation of ILlmService
    /// </summary>
    public class AzureOpenAiLlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmOptions _options;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public LlmCapabilities Capabilities { get; }

        public bool IsConfigured => 
            !string.IsNullOrWhiteSpace(_options.ApiKey) && 
            !string.IsNullOrWhiteSpace(_options.Deployment);

        public AzureOpenAiLlmService(LlmOptions options, HttpClient? httpClient = null)
        {
            Guard.AgainstNull(options, nameof(options));
            
            _options = options;
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            // Configure retry policy
            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => 
                    r.StatusCode == HttpStatusCode.TooManyRequests || 
                    r.StatusCode >= HttpStatusCode.InternalServerError)
                .WaitAndRetryAsync(
                    _options.MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + 
                                   TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));

            Capabilities = new LlmCapabilities
            {
                MaxContextTokens = 8192, // Azure OpenAI default
                MaxOutputTokens = 4096,
                SupportsStreaming = true,
                SupportsImages = false,
                SupportsJsonSchema = true,
                SupportsFunctionCalling = true
            };
        }

        public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(request, nameof(request));

            var stopwatch = Stopwatch.StartNew();

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                temperature = request.Temperature ?? _options.Temperature,
                top_p = request.TopP ?? _options.TopP,
                max_tokens = request.MaxTokens ?? _options.MaxTokens,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = $"/openai/deployments/{_options.Deployment}/chat/completions?api-version=2024-02-15-preview";
            
            var response = await _retryPolicy.ExecuteAsync(async ct => 
                await _httpClient.PostAsync(endpoint, content, ct), 
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseData = JsonSerializer.Deserialize<AzureOpenAiChatResponse>(responseJson);

            stopwatch.Stop();

            if (responseData?.Choices == null || responseData.Choices.Count == 0)
                throw new InvalidOperationException("No response from Azure OpenAI");

            var usage = responseData.Usage ?? new AzureOpenAiUsage();
            var cost = CalculateCost(usage.PromptTokens, usage.CompletionTokens);

            return new LlmResponse
            {
                Text = responseData.Choices[0].Message?.Content ?? string.Empty,
                PromptTokens = usage.PromptTokens,
                CompletionTokens = usage.CompletionTokens,
                TotalTokens = usage.TotalTokens,
                EstimatedCostUsd = cost,
                Duration = stopwatch.Elapsed,
                FinishReason = responseData.Choices[0].FinishReason
            };
        }

        public async IAsyncEnumerable<LlmDelta> StreamAsync(
            LlmRequest request, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(request, nameof(request));

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                temperature = request.Temperature ?? _options.Temperature,
                top_p = request.TopP ?? _options.TopP,
                max_tokens = request.MaxTokens ?? _options.MaxTokens,
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);

            var endpoint = $"/openai/deployments/{_options.Deployment}/chat/completions?api-version=2024-02-15-preview";
            
            var response = await _retryPolicy.ExecuteAsync(
                async ct => 
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(endpoint, content, ct);
                }, 
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var data = line.Substring(6).Trim();
                
                if (data == "[DONE]")
                {
                    yield return new LlmDelta { IsComplete = true };
                    yield break;
                }

                AzureOpenAiStreamChunk? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<AzureOpenAiStreamChunk>(data);
                }
                catch
                {
                    continue;
                }

                if (chunk?.Choices == null || chunk.Choices.Count == 0)
                    continue;

                var choice = chunk.Choices[0];
                var delta = new LlmDelta
                {
                    Content = choice.Delta?.Content ?? string.Empty,
                    FinishReason = choice.FinishReason
                };

                yield return delta;
            }
        }

        public async Task<ModerationResult> ModerateAsync(string input, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNullOrWhiteSpace(input, nameof(input));

            // Azure OpenAI doesn't have a built-in moderation endpoint like OpenAI
            // Implement basic content filtering here or use Azure Content Safety API
            await Task.CompletedTask;

            var flaggedKeywords = new[] { "hate", "violence", "sexual", "self-harm" };
            var lowerInput = input.ToLowerInvariant();
            var flagged = false;

            foreach (var keyword in flaggedKeywords)
            {
                if (lowerInput.Contains(keyword))
                {
                    flagged = true;
                    break;
                }
            }

            return new ModerationResult
            {
                IsFlagged = flagged,
                Categories = flagged ? new[] { "content-policy" } : Array.Empty<string>(),
                Message = flagged 
                    ? "Content may violate policy (basic keyword check)"
                    : "Content passed basic moderation"
            };
        }

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new LlmRequest
                {
                    SystemPrompt = "You are a test assistant.",
                    UserPrompt = "Say 'OK' if you can hear me.",
                    MaxTokens = 10
                };

                var response = await CompleteAsync(request, cancellationToken);
                return !string.IsNullOrWhiteSpace(response.Text);
            }
            catch
            {
                return false;
            }
        }

        private static double CalculateCost(int promptTokens, int completionTokens)
        {
            // Azure OpenAI pricing (approximate, may vary by region)
            const double promptCost = 0.03;  // per 1K tokens
            const double completionCost = 0.06; // per 1K tokens

            return (promptTokens / 1000.0 * promptCost) + (completionTokens / 1000.0 * completionCost);
        }

        // Internal DTOs for Azure OpenAI API
        private class AzureOpenAiChatResponse
        {
            [JsonPropertyName("choices")]
            public List<AzureOpenAiChoice> Choices { get; set; } = new();

            [JsonPropertyName("usage")]
            public AzureOpenAiUsage? Usage { get; set; }
        }

        private class AzureOpenAiChoice
        {
            [JsonPropertyName("message")]
            public AzureOpenAiMessage? Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private class AzureOpenAiMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        private class AzureOpenAiUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }

        private class AzureOpenAiStreamChunk
        {
            [JsonPropertyName("choices")]
            public List<AzureOpenAiStreamChoice> Choices { get; set; } = new();
        }

        private class AzureOpenAiStreamChoice
        {
            [JsonPropertyName("delta")]
            public AzureOpenAiDelta? Delta { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private class AzureOpenAiDelta
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
