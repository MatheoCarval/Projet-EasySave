namespace EasyLog.Loggers;

using EasyLog.Core;
using EasyLog.Enums;
using EasyLog.Exceptions;
using System.IO;
using System.Text;

public class JsonLogger : LoggerBase
{
    public JsonLogger(string outputPath) : base(outputPath, LogFormat.JSON)
    {
    }

    /// <summary>
    /// Écrit le contenu formaté dans le fichier JSON
    /// LOGIQUE:
    /// - Utilise UTF-8 sans BOM pour compatibilité maximale
    /// - Écrase le fichier existant (le contenu est déjà merged dans LoggerBase)
    /// </summary>
    protected override void WriteToFile(string content, string path)
    {
        try
        {
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