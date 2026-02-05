using EasyLog.Exceptions;
using Xunit;
using System;

namespace EasySave.Tests.EasyLog.Exceptions
{
    public class LoggerExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Test logger error";

            // Act
            var exception = new LoggerException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsBoth()
        {
            // Arrange
            var message = "Test logger error";
            var innerException = new IOException("Inner error");

            // Act
            var exception = new LoggerException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_Default_CreatesException()
        {
            // Arrange & Act
            var exception = new LoggerException();

            // Assert
            Assert.NotNull(exception);
        }
    }
}
