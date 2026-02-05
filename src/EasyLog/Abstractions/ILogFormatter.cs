namespace EasyLog.Abstractions;

/// <summary>
/// Defines the contract for formatting and parsing generic type T to and from serialized content (JSON, XML, etc.).
/// </summary>
public interface ILogFormatter<T>
{
    /// <summary>
    /// Converts a single object of type T to formatted content string.
    /// </summary>
    string Format(T data);

    /// <summary>
    /// Converts a collection of objects of type T to formatted content string.
    /// </summary>
    string FormatCollection(IEnumerable<T> data);

    /// <summary>
    /// Parses a formatted content string and deserializes it into a single object of type T.
    /// </summary>
    T Parse(string content);

    /// <summary>
    /// Parses a formatted content string containing a collection and deserializes it into an enumerable of type T.
    /// </summary>
    IEnumerable<T> ParseCollection(string content);
}
