using System;

namespace AzureTtsBatchStudio.Infrastructure.Llm
{
    /// <summary>
    /// Configuration options for LLM services
    /// </summary>
    public class LlmOptions
    {
        public string Provider { get; set; } = "OpenAI"; // "OpenAI" or "AzureOpenAI"
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public string Deployment { get; set; } = string.Empty; // Azure OpenAI deployment name
        public double Temperature { get; set; } = 0.8;
        public double TopP { get; set; } = 0.9;
        public int MaxTokens { get; set; } = 4000;
        public int TimeoutSeconds { get; set; } = 120;
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// Request for LLM generation
    /// </summary>
    public class LlmRequest
    {
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public int? MaxTokens { get; set; }
        public string? Model { get; set; }
    }

    /// <summary>
    /// Response from LLM generation
    /// </summary>
    public class LlmResponse
    {
        public string Text { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public double EstimatedCostUsd { get; set; }
        public TimeSpan Duration { get; set; }
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Streaming delta from LLM
    /// </summary>
    public class LlmDelta
    {
        public string Content { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Capabilities of the LLM service
    /// </summary>
    public class LlmCapabilities
    {
        public int MaxContextTokens { get; set; }
        public int MaxOutputTokens { get; set; }
        public bool SupportsStreaming { get; set; }
        public bool SupportsImages { get; set; }
        public bool SupportsJsonSchema { get; set; }
        public bool SupportsFunctionCalling { get; set; }
    }

    /// <summary>
    /// Result from content moderation
    /// </summary>
    public class ModerationResult
    {
        public bool IsFlagged { get; set; }
        public string[] Categories { get; set; } = Array.Empty<string>();
        public string Message { get; set; } = string.Empty;
        public double MaxScore { get; set; }
    }
}
