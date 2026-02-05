namespace EasyLog.Loggers;

using EasyLog.Core;
using EasyLog.Enums;
using EasyLog.Exceptions;
using System;
using System.IO;
using System.Text;

/// <summary>
/// Logger implementation that persists log entries in JSON format using UTF-8 encoding without byte order mark.
/// </summary>
public class JsonLogger : LoggerBase
{
    private readonly string _baseOutputPath;

    /// <summary>
    /// Initializes a new instance of the JsonLogger class with the specified output file path.
    /// </summary>
    public JsonLogger(string outputPath) : base(GetDailyLogPath(outputPath), LogFormat.JSON)
    {
        _baseOutputPath = outputPath;
    }

    /// <summary>
    /// Génère le chemin du fichier de log journalier
    /// Format: {baseOutputPath}_YYYY-MM-DD.json
    /// </summary>
    private static string GetDailyLogPath(string baseOutputPath)
    {
        string directory = Path.GetDirectoryName(baseOutputPath) ?? string.Empty;
        string filenameWithoutExt = Path.GetFileNameWithoutExtension(baseOutputPath);
        string dateString = DateTime.Now.ToString("yyyy-MM-dd");

        return Path.Combine(directory, $"{filenameWithoutExt}_{dateString}.json");
    }

    /// <summary>
    /// Writes formatted JSON content to the specified file path using UTF-8 encoding without byte order mark. The content is already merged in LoggerBase before writing.
    /// Handles daily log file rotation by checking if the current date has changed.
    /// </summary>
    protected override void WriteToFile(string content, string path)
    {
        try
        {
            // Vérifier si on doit changer de fichier (nouveau jour)
            string currentDailyPath = GetDailyLogPath(_baseOutputPath);
            if (currentDailyPath != path)
            {
                // Mettre à jour le chemin dans la classe de base
                typeof(LoggerBase).GetField("_outputPath",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                    ?.SetValue(this, currentDailyPath);
                path = currentDailyPath;
            }

            // UTF-8 sans BOM pour compatibilité
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
        catch (IOException ex)
        {
            throw new LoggerException($"Failed to write to file {path}", ex);
        }
    }

    /// <summary>
    /// Reads JSON content from the specified file path using UTF-8 encoding. Returns an empty string if the file does not exist.
    /// Always reads from the current daily log file.
    /// </summary>
    protected override string ReadFromFile(string path)
    {
        try
        {
            // Lire le fichier du jour courant
            string currentDailyPath = GetDailyLogPath(_baseOutputPath);

            if (!File.Exists(currentDailyPath))
                return string.Empty;

            return File.ReadAllText(currentDailyPath, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            throw new LoggerException($"Failed to read from file {path}", ex);
        }
    }
}