using EasySave.Exceptions;
using Xunit;
using System;

namespace EasySave.Tests.Exceptions
{
    public class FileTransferExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Test error message";

            // Act
            var exception = new FileTransferException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsBoth()
        {
            // Arrange
            var message = "Test error message";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new FileTransferException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }
    }
}