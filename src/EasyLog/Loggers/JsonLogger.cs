namespace EasyLog.Loggers;

using EasyLog.Core;
using EasyLog.Enums;
using EasyLog.Exceptions;
using System.IO;
using System.Text;

/// <summary>
/// Logger implementation that persists log entries in JSON format using UTF-8 encoding without byte order mark.
/// </summary>
public class JsonLogger : LoggerBase
{
    /// <summary>
    /// Initializes a new instance of the JsonLogger class with the specified output file path.
    /// </summary>
    public JsonLogger(string outputPath) : base(outputPath, LogFormat.JSON)
    {
    }

    /// <summary>
    /// Writes formatted JSON content to the specified file path using UTF-8 encoding without byte order mark. The content is already merged in LoggerBase before writing.
    /// </summary>
    protected override void WriteToFile(string content, string path)
    {
        try
        {
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
        catch (IOException ex)
        {
            throw new LoggerException($"Failed to write to file {path}", ex);
        }
    }

    /// <summary>
    /// Reads JSON content from the specified file path using UTF-8 encoding. Returns an empty string if the file does not exist.
    /// </summary>
    protected override string ReadFromFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return string.Empty;

            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            throw new LoggerException($"Failed to read from file {path}", ex);
        }
    }
}