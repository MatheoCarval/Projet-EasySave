using System;

namespace EasyLog.Exceptions;

/// <summary>
/// Exception thrown when an error occurs during logging operations, including file I/O failures or serialization errors.
/// </summary>
public class LoggerException : Exception
{
    /// <summary>
    /// Initializes a new instance of LoggerException with no error message.
    /// </summary>
    public LoggerException()
    {
    }

    /// <summary>
    /// Initializes a new instance of LoggerException with a specified error message.
    /// </summary>
    public LoggerException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of LoggerException with a specified error message and a reference to the inner exception that caused this exception.
    /// </summary>
    public LoggerException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
