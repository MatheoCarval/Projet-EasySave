namespace EasyLog.Loggers;

using EasyLog.Core;
using EasyLog.Enums;
using EasyLog.Exceptions;
using System.IO;
using System.Text;
/// <summary>
/// Logger implementation that persists log entries in XML format using UTF-8 encoding without BOM.
/// </summary>
public class XmlLogger : LoggerBase
{
    /// <summary>
    /// Initializes a new instance of the XmlLogger class with the specified output file path.
    /// </summary>
    public XmlLogger(string outputPath) : base(outputPath, LogFormat.XML)
    {
    }

    /// <summary>
    /// Writes formatted XML content to the specified file path using UTF-8 encoding without byte order mark. Throws LoggerException on IO failures.
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
    /// Reads XML content from the specified file path using UTF-8 encoding. Returns an empty string if the file does not exist. Throws LoggerException on IO failures.
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