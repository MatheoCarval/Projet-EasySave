using System;

namespace EasyLog.Exceptions;

/// <summary>
/// Exception thrown when an error occurs during log entry formatting or serialization operations.
/// </summary>
public class FormatterException : Exception
{
    /// <summary>
    /// Initializes a new instance of FormatterException with no error message.
    /// </summary>
    public FormatterException()
    {
    }

    /// <summary>
    /// Initializes a new instance of FormatterException with a specified error message.
    /// </summary>
    public FormatterException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of FormatterException with a specified error message and a reference to the inner exception that caused this exception.
    /// </summary>
    public FormatterException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
