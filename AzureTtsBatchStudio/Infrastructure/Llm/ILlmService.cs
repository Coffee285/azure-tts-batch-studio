using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzureTtsBatchStudio.Infrastructure.Llm
{
    /// <summary>
    /// Provider-agnostic LLM service interface
    /// </summary>
    public interface ILlmService
    {
        /// <summary>
        /// Stream completion tokens as they are generated
        /// </summary>
        IAsyncEnumerable<LlmDelta> StreamAsync(LlmRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Complete the request and return the full response
        /// </summary>
        Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check content for policy violations
        /// </summary>
        Task<ModerationResult> ModerateAsync(string input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the capabilities of this LLM service
        /// </summary>
        LlmCapabilities Capabilities { get; }

        /// <summary>
        /// Test connection to the LLM service
        /// </summary>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if the service is configured
        /// </summary>
        bool IsConfigured { get; }
    }
}
