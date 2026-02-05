using EasyLog.Exceptions;
using Xunit;
using System;

namespace EasySave.Tests.EasyLog.Exceptions
{
    /// <summary>
    /// Unit tests for the LoggerException class, verifying exception initialization with messages and inner exceptions.
    /// </summary>
    public class LoggerExceptionTests
    {
        /// <summary>
        /// Verifies that LoggerException constructor with a message parameter correctly sets the exception message.
        /// </summary>
        [Fact]
        public void Constructor_WithMessage_SetsMessage()
        {
            var message = "Test logger error";

            var exception = new LoggerException(message);
            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// Verifies that LoggerException constructor with message and inner exception parameters correctly sets both properties.
        /// </summary>
        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsBoth()
        {
            var message = "Test logger error";
            var innerException = new IOException("Inner error");

            var exception = new LoggerException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        /// <summary>
        /// Verifies that LoggerException can be instantiated with the default constructor and creates a valid exception instance.
        /// </summary>
        [Fact]
        public void Constructor_Default_CreatesException()
        {
            var exception = new LoggerException();

            Assert.NotNull(exception);
        }
    }
}
