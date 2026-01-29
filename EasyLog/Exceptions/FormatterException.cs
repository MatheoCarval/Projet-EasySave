using System;

namespace EasyLog.Exceptions;

public class FormatterException : Exception
{
    public FormatterException()
    {
    }

    public FormatterException(string message)
        : base(message)
    {
    }

    public FormatterException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
