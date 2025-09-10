using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    public interface IOpenAIClient
    {
        Task<IAsyncEnumerable<GenerationProgress>> GenerateStreamAsync(
            OpenAIGenerationRequest request, 
            CancellationToken cancellationToken = default);
        Task<GenerationProgress> GenerateAsync(
            OpenAIGenerationRequest request, 
            CancellationToken cancellationToken = default);
        void ConfigureApiKey(string apiKey);
        bool IsConfigured { get; }
    }

    public class OpenAIClient : IOpenAIClient
    {
        private readonly HttpClient _httpClient;
        private string? _apiKey;
        private const string BaseUrl = "https://api.openai.com/v1";

        public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

        public OpenAIClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public void ConfigureApiKey(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Azure-TTS-Batch-Studio/1.0");
        }

        public async Task<GenerationProgress> GenerateAsync(
            OpenAIGenerationRequest request, 
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenAI API key not configured");

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Create non-streaming request
                var requestData = new
                {
                    model = request.Model,
                    messages = request.Messages.ConvertAll(m => new { role = m.Role, content = m.Content }),
                    temperature = request.Temperature,
                    top_p = request.TopP,
                    max_tokens = request.MaxTokens,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                var generatedText = responseData
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                TokenUsage? tokenUsage = null;
                if (responseData.TryGetProperty("usage", out var usageElement))
                {
                    tokenUsage = new TokenUsage
                    {
                        PromptTokens = usageElement.GetProperty("prompt_tokens").GetInt32(),
                        CompletionTokens = usageElement.GetProperty("completion_tokens").GetInt32(),
                        TotalTokens = usageElement.GetProperty("total_tokens").GetInt32()
                    };
                }

                return new GenerationProgress
                {
                    Text = generatedText,
                    IsComplete = true,
                    TokenUsage = tokenUsage,
                    Elapsed = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new GenerationProgress
                {
                    Text = string.Empty,
                    IsComplete = true,
                    Error = ex.Message,
                    Elapsed = DateTime.UtcNow - startTime
                };
            }
        }

        public async Task<IAsyncEnumerable<GenerationProgress>> GenerateStreamAsync(
            OpenAIGenerationRequest request, 
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenAI API key not configured");

            return GenerateStreamAsyncImpl(request, cancellationToken);
        }

        private async IAsyncEnumerable<GenerationProgress> GenerateStreamAsyncImpl(
            OpenAIGenerationRequest request, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var accumulatedText = new StringBuilder();

            // Setup request
            var requestData = new
            {
                model = request.Model,
                messages = request.Messages.ConvertAll(m => new { role = m.Role, content = m.Content }),
                temperature = request.Temperature,
                top_p = request.TopP,
                max_tokens = request.MaxTokens,
                stream = true
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            Stream stream;
            StreamReader reader;
            
            // Make request - any exception here will propagate up
            response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            reader = new StreamReader(stream);

            // Process streaming response - use using statements for cleanup
            using (response)
            using (stream)
            using (reader)
            {
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);
                        if (data == "[DONE]")
                        {
                            yield return new GenerationProgress
                            {
                                Text = accumulatedText.ToString(),
                                IsComplete = true,
                                Elapsed = DateTime.UtcNow - startTime
                            };
                            yield break;
                        }

                        // Handle JSON parsing safely without exceptions in yield context
                        if (TryParseStreamChunk(data, out var deltaContent))
                        {
                            if (!string.IsNullOrEmpty(deltaContent))
                            {
                                accumulatedText.Append(deltaContent);
                                
                                yield return new GenerationProgress
                                {
                                    Text = accumulatedText.ToString(),
                                    IsComplete = false,
                                    Elapsed = DateTime.UtcNow - startTime
                                };
                            }
                        }
                    }
                }
            }
        }

        private static bool TryParseStreamChunk(string data, out string deltaContent)
        {
            deltaContent = string.Empty;
            
            try
            {
                var eventData = JsonSerializer.Deserialize<JsonElement>(data);
                if (eventData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var choice = choices[0];
                    if (choice.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var contentElement))
                    {
                        deltaContent = contentElement.GetString() ?? string.Empty;
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // Skip malformed JSON chunks
                return false;
            }
            
            return false;
        }
    }
}