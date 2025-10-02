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
    /// OpenAI implementation of ILlmService with streaming, retries, and cost tracking
    /// </summary>
    public class OpenAiLlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmOptions _options;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public LlmCapabilities Capabilities { get; }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public OpenAiLlmService(LlmOptions options, HttpClient? httpClient = null)
        {
            Guard.AgainstNull(options, nameof(options));
            
            _options = options;
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            // Configure retry policy with exponential backoff
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
                MaxContextTokens = GetMaxContextTokens(_options.Model),
                MaxOutputTokens = 4096,
                SupportsStreaming = true,
                SupportsImages = _options.Model.Contains("vision"),
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
                model = request.Model ?? _options.Model,
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

            using (var response = await _retryPolicy.ExecuteAsync(async ct =>
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                return await _httpClient.PostAsync("chat/completions", content, ct);
            }, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<OpenAiChatResponse>(responseJson);

                stopwatch.Stop();

                if (responseData?.Choices == null || responseData.Choices.Count == 0)
                    throw new InvalidOperationException("No response from OpenAI");

                var usage = responseData.Usage ?? new OpenAiUsage();
                var cost = CalculateCost(usage.PromptTokens, usage.CompletionTokens, request.Model ?? _options.Model);

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
        }

        public async IAsyncEnumerable<LlmDelta> StreamAsync(
            LlmRequest request, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(request, nameof(request));

            var requestBody = new
            {
                model = request.Model ?? _options.Model,
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

            var response = await _retryPolicy.ExecuteAsync(
                async ct =>
                {
                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                    {
                        Content = content
                    };
                    requestMessage.Headers.Accept.Clear();
                    requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
                    return await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
                },
                cancellationToken);

            await using (response)
            {
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

                    OpenAiStreamChunk? chunk;
                    try
                    {
                        chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data);
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
        }

        public async Task<ModerationResult> ModerateAsync(string input, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNullOrWhiteSpace(input, nameof(input));

            try
            {
                var requestBody = new { input, model = "text-moderation-latest" };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("moderations", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var moderationData = JsonSerializer.Deserialize<OpenAiModerationResponse>(responseJson);

                if (moderationData?.Results == null || moderationData.Results.Count == 0)
                    return new ModerationResult { IsFlagged = false };

                var result = moderationData.Results[0];
                var flaggedCategories = new List<string>();
                double maxScore = 0;

                foreach (var kvp in result.CategoryScores)
                {
                    if (kvp.Value > 0.5) // Threshold for flagging
                    {
                        flaggedCategories.Add(kvp.Key);
                        maxScore = Math.Max(maxScore, kvp.Value);
                    }
                }

                return new ModerationResult
                {
                    IsFlagged = result.Flagged,
                    Categories = flaggedCategories.ToArray(),
                    Message = result.Flagged 
                        ? $"Content flagged for: {string.Join(", ", flaggedCategories)}"
                        : "Content passed moderation",
                    MaxScore = maxScore
                };
            }
            catch (Exception ex)
            {
                // If moderation fails, log but don't block
                return new ModerationResult
                {
                    IsFlagged = false,
                    Message = $"Moderation check failed: {ex.Message}"
                };
            }
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

        private static int GetMaxContextTokens(string model)
        {
            return model switch
            {
                _ when model.Contains("gpt-4-turbo") => 128000,
                _ when model.Contains("gpt-4-32k") => 32768,
                _ when model.Contains("gpt-4") => 8192,
                _ when model.Contains("gpt-3.5-turbo-16k") => 16384,
                _ when model.Contains("gpt-3.5-turbo") => 4096,
                _ => 8192
            };
        }

        private static double CalculateCost(int promptTokens, int completionTokens, string model)
        {
            // Approximate costs per 1K tokens (as of 2024)
            var (promptCost, completionCost) = model switch
            {
                _ when model.Contains("gpt-4-turbo") => (0.01, 0.03),
                _ when model.Contains("gpt-4") => (0.03, 0.06),
                _ when model.Contains("gpt-3.5-turbo") => (0.0015, 0.002),
                _ => (0.01, 0.03)
            };

            return (promptTokens / 1000.0 * promptCost) + (completionTokens / 1000.0 * completionCost);
        }

        // Internal DTOs for OpenAI API
        private class OpenAiChatResponse
        {
            [JsonPropertyName("choices")]
            public List<OpenAiChoice> Choices { get; set; } = new();

            [JsonPropertyName("usage")]
            public OpenAiUsage? Usage { get; set; }
        }

        private class OpenAiChoice
        {
            [JsonPropertyName("message")]
            public OpenAiMessage? Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private class OpenAiMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        private class OpenAiUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }

        private class OpenAiStreamChunk
        {
            [JsonPropertyName("choices")]
            public List<OpenAiStreamChoice> Choices { get; set; } = new();
        }

        private class OpenAiStreamChoice
        {
            [JsonPropertyName("delta")]
            public OpenAiDelta? Delta { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private class OpenAiDelta
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        private class OpenAiModerationResponse
        {
            [JsonPropertyName("results")]
            public List<OpenAiModerationResult> Results { get; set; } = new();
        }

        private class OpenAiModerationResult
        {
            [JsonPropertyName("flagged")]
            public bool Flagged { get; set; }

            [JsonPropertyName("categories")]
            public Dictionary<string, bool> Categories { get; set; } = new();

            [JsonPropertyName("category_scores")]
            public Dictionary<string, double> CategoryScores { get; set; } = new();
        }
    }
}
