using System;
using AzureTtsBatchStudio.Services;
using Xunit;

namespace AzureTtsBatchStudio.Tests
{
    public class TtsExceptionTests
    {
        [Fact]
        public void TtsException_ShouldSetErrorTypeAndMessage()
        {
            // Arrange
            var errorType = TtsErrorType.PayloadTooLarge;
            var message = "Test message";
            var errorCode = "413";

            // Act
            var exception = new TtsException(errorType, message, errorCode);

            // Assert
            Assert.Equal(errorType, exception.ErrorType);
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
        }

        [Fact]
        public void TtsException_ShouldWrapInnerException()
        {
            // Arrange
            var errorType = TtsErrorType.NetworkError;
            var message = "Network error occurred";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new TtsException(errorType, message, innerException);

            // Assert
            Assert.Equal(errorType, exception.ErrorType);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Theory]
        [InlineData(TtsErrorType.PayloadTooLarge)]
        [InlineData(TtsErrorType.RateLimited)]
        [InlineData(TtsErrorType.ServiceUnavailable)]
        [InlineData(TtsErrorType.InvalidRequest)]
        [InlineData(TtsErrorType.NetworkError)]
        [InlineData(TtsErrorType.Unknown)]
        public void TtsException_ShouldSupportAllErrorTypes(TtsErrorType errorType)
        {
            // Arrange & Act
            var exception = new TtsException(errorType, "Test message");

            // Assert
            Assert.Equal(errorType, exception.ErrorType);
        }
    }
}