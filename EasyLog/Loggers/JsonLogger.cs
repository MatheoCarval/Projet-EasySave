namespace EasyLog.Loggers;

using EasyLog.Core;
using EasyLog.Enums;
using EasyLog.Exceptions;
using System;
using System.IO;
using System.Text;

public class JsonLogger : LoggerBase
{
    private readonly string _baseOutputPath;

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
    /// Écrit le contenu formaté dans le fichier JSON
    /// LOGIQUE:
    /// - Utilise UTF-8 sans BOM pour compatibilité maximale
    /// - Écrase le fichier existant (le contenu est déjà merged dans LoggerBase)
    /// - Le fichier est spécifique au jour courant
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
    /// Lit le contenu du fichier JSON
    /// Retourne string.Empty si le fichier n'existe pas
    /// Lit toujours le fichier du jour courant
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