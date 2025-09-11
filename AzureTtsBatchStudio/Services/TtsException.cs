using System;

namespace AzureTtsBatchStudio.Services
{
    public enum TtsErrorType
    {
        Unknown,
        PayloadTooLarge,
        RateLimited,
        ServiceUnavailable,
        InvalidRequest,
        NetworkError
    }

    public class TtsException : Exception
    {
        public TtsErrorType ErrorType { get; }
        public string? ErrorCode { get; }

        public TtsException(TtsErrorType errorType, string message, string? errorCode = null) 
            : base(message)
        {
            ErrorType = errorType;
            ErrorCode = errorCode;
        }

        public TtsException(TtsErrorType errorType, string message, Exception innerException, string? errorCode = null) 
            : base(message, innerException)
        {
            ErrorType = errorType;
            ErrorCode = errorCode;
        }
    }
}