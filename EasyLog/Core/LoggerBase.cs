namespace EasyLog.Core;

using EasyLog.Abstractions;
using EasyLog.Enums;
using EasyLog.Exceptions;
using EasyLog.Formatters;

public abstract class LoggerBase : ILogger
{
    /// <summary>
    /// The file path where log entries are persisted.
    /// </summary>
    protected string _outputPath;

    /// <summary>
    /// The current logging format (JSON, XML, etc.).
    /// </summary>
    protected LogFormat _format;

    /// <summary>
    /// Dictionary of custom formatters indexed by type. Enables type-specific formatting logic.
    /// </summary>
    private readonly Dictionary<Type, object> _formatters;

    /// <summary>
    /// Lock object for thread-safe concurrent write operations.
    /// </summary>
    protected readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the LoggerBase class with an output path and logging format. Ensures the output directory exists.
    /// </summary>
    protected LoggerBase(string outputPath, LogFormat format = LogFormat.JSON)
    {
        _outputPath = outputPath;
        _format = format;
        _formatters = new Dictionary<Type, object>();

        EnsureDirectoryExists(_outputPath);
    }

    /// <summary>
    /// Registers a custom formatter for a specific type to override default formatting behavior.
    /// </summary>
    public void RegisterFormatter<T>(ILogFormatter<T> formatter) where T : class
    {
        Type type = typeof(T);
        _formatters[type] = formatter;
    }

    /// <summary>
    /// Retrieves or creates a formatter for type T. Returns a registered custom formatter if available; otherwise creates a default formatter based on the current format setting.
    /// </summary>
    protected ILogFormatter<T> GetFormatter<T>() where T : class
    {
        Type type = typeof(T);

        if (_formatters.ContainsKey(type))
        {
            return (ILogFormatter<T>)_formatters[type];
        }

        ILogFormatter<T> defaultFormatter = _format switch
        {
            LogFormat.JSON => new JsonFormatter<T>(prettyPrint: true, paginate: true),
            LogFormat.XML => new XmlFormatter<T>(indent: true),
            _ => throw new NotSupportedException($"Format {_format} not supported")
        };

        _formatters[type] = defaultFormatter;
        return defaultFormatter;
    }

    /// <summary>
    /// Logs a single object of type T. Retrieves the appropriate formatter, reads existing log content, appends the new entry, and writes the complete collection back to the file in a thread-safe manner.
    /// </summary>
    public void Log<T>(T data) where T : class
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        lock (_lock)
        {
            try
            {
                var formatter = GetFormatter<T>();

                var existingContent = ReadFromFile(_outputPath);
                var existingData = new List<T>();

                if (!string.IsNullOrWhiteSpace(existingContent))
                {
                    try
                    {
                        existingData = formatter.ParseCollection(existingContent).ToList();
                    }
                    catch
                    {
                        existingData = new List<T>();
                    }
                }

                existingData.Add(data);

                string formattedContent = formatter.FormatCollection(existingData);

                WriteToFile(formattedContent, _outputPath);
            }
            catch (Exception ex)
            {
                throw new LoggerException($"Error logging data of type {typeof(T).Name}", ex);
            }
        }
    }

    /// <summary>
    /// Logs a collection of objects of type T. More efficient than calling Log() multiple times by batching all entries in a single write operation.
    /// </summary>
    public void LogCollection<T>(IEnumerable<T> data) where T : class
    {
        if (data == null || !data.Any())
            return;

        lock (_lock)
        {
            try
            {
                var formatter = GetFormatter<T>();

                var existingContent = ReadFromFile(_outputPath);
                var existingData = new List<T>();

                if (!string.IsNullOrWhiteSpace(existingContent))
                {
                    try
                    {
                        existingData = formatter.ParseCollection(existingContent).ToList();
                    }
                    catch
                    {
                        existingData = new List<T>();
                    }
                }

                existingData.AddRange(data);

                string formattedContent = formatter.FormatCollection(existingData);
                WriteToFile(formattedContent, _outputPath);
            }
            catch (Exception ex)
            {
                throw new LoggerException($"Error logging collection of type {typeof(T).Name}", ex);
            }
        }
    }

    /// <summary>
    /// Changes the output file path for logging operations and ensures the target directory exists.
    /// </summary>
    public void SetOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        _outputPath = path;
        EnsureDirectoryExists(_outputPath);
    }

    /// <summary>
    /// Changes the logging format and clears cached formatters to force recreation with the new format.
    /// </summary>
    public void SetFormat(LogFormat format)
    {
        _format = format;
        _formatters.Clear();
    }

    /// <summary>
    /// Reads and retrieves all logged entries of type T from the log file, returning an empty collection if the file is empty or unreadable.
    /// </summary>
    public IEnumerable<T> ReadLog<T>() where T : class
    {
        lock (_lock)
        {
            try
            {
                var content = ReadFromFile(_outputPath);
                if (string.IsNullOrWhiteSpace(content))
                    return Enumerable.Empty<T>();

                var formatter = GetFormatter<T>();
                return formatter.ParseCollection(content);
            }
            catch (Exception ex)
            {
                throw new LoggerException($"Error reading log of type {typeof(T).Name}", ex);
            }
        }
    }

    /// <summary>
    /// Flushes any buffered data to the underlying storage. Default implementation is empty; derived classes with buffering can override this method.
    /// </summary>
    public virtual void Flush()
    {
    }

    /// <summary>
    /// Abstract method that derived classes implement to define how formatted content is physically written to a file.
    /// </summary>
    protected abstract void WriteToFile(string content, string path);

    /// <summary>
    /// Abstract method that derived classes implement to define how content is physically read from a file.
    /// </summary>
    protected abstract string ReadFromFile(string path);

    /// <summary>
    /// Ensures that the directory containing the specified file path exists; creates it if necessary.
    /// </summary>
    protected void EnsureDirectoryExists(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

}