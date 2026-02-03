namespace EasyLog.Loggers;

using EasyLog.Core;
using EasyLog.Enums;
using EasyLog.Exceptions;
using System.IO;
using System.Text;
public class XmlLogger : LoggerBase
{
    public XmlLogger(string outputPath) : base(outputPath, LogFormat.XML)
    {
    }

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