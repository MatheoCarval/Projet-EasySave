using EasyLog.Exceptions;
using Xunit;
using System;

namespace EasySave.Tests.EasyLog.Exceptions
{
    public class FormatterExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Test formatter error";

            // Act
            var exception = new FormatterException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsBoth()
        {
            // Arrange
            var message = "Test formatter error";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new FormatterException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_Default_CreatesException()
        {
            // Arrange & Act
            var exception = new FormatterException();

            // Assert
            Assert.NotNull(exception);
        }
    }
}
