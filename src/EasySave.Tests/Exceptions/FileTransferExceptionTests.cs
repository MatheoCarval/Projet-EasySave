using EasySave.Exceptions;
using Xunit;
using System;

namespace EasySave.Tests.Exceptions
{
    /// <summary>
    /// Unit tests for the FileTransferException class, verifying exception initialization with messages and inner exceptions.
    /// </summary>
    public class FileTransferExceptionTests
    {
        /// <summary>
        /// Verifies that FileTransferException constructor with a message parameter correctly sets the exception message.
        /// </summary>
        [Fact]
        public void Constructor_WithMessage_SetsMessage()
        {
            var message = "Test error message";

            var exception = new FileTransferException(message);

            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// Verifies that FileTransferException constructor with message and inner exception parameters correctly sets both properties.
        /// </summary>
        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsBoth()
        {
            var message = "Test error message";
            var innerException = new InvalidOperationException("Inner error");

            var exception = new FileTransferException(message, innerException);

            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }
    }
}