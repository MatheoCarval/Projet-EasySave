namespace EasyLog.Abstractions;

/// <summary>
/// Defines the contract for logging operations, including writing, reading, and managing log data.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a single data object of generic type.
    /// </summary>
    void Log<T>(T data) where T : class;

    /// <summary>
    /// Logs a collection of data objects of generic type.
    /// </summary>
    void LogCollection<T>(IEnumerable<T> data) where T : class;

    /// <summary>
    /// Flushes any pending log data, ensuring all logs are written to storage.
    /// </summary>
    void Flush();

    /// <summary>
    /// Reads and returns all logged data of the specified generic type from storage.
    /// </summary>
    IEnumerable<T> ReadLog<T>() where T : class;
}