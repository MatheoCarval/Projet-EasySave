using EasyLog.Exceptions;
using Xunit;
using System;

namespace EasySave.Tests.EasyLog.Exceptions
{
    /// <summary>
    /// Unit tests for the FormatterException class, verifying exception initialization with messages and inner exceptions.
    /// </summary>
    public class FormatterExceptionTests
    {
        /// <summary>
        /// Verifies that FormatterException constructor with a message parameter correctly sets the exception message.
        /// </summary>
        [Fact]
        public void Constructor_WithMessage_SetsMessage()
        {
            var message = "Test formatter error";

            var exception = new FormatterException(message);
            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// Verifies that FormatterException constructor with message and inner exception parameters correctly sets both properties.
        /// </summary>
        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsBoth()
        {
            var message = "Test formatter error";
            var innerException = new InvalidOperationException("Inner error");

            var exception = new FormatterException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        /// <summary>
        /// Verifies that FormatterException can be instantiated with the default constructor and creates a valid exception instance.
        /// </summary>
        [Fact]
        public void Constructor_Default_CreatesException()
        {
            var exception = new FormatterException();

            Assert.NotNull(exception);
        }
    }
}
