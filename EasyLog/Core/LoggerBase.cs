namespace EasyLog.Core;

using EasyLog.Abstractions;
using EasyLog.Enums;
using EasyLog.Exceptions;
using EasyLog.Formatters;

public abstract class LoggerBase : ILogger
{
    // Chemin du fichier de sortie
    protected string _outputPath;

    // Format actuel (JSON, XML, etc.)
    protected LogFormat _format;

    // Dictionnaire des formatters personnalisés par type
    // Key = Type de l'objet (ex: typeof(BackupLogEntry))
    // Value = Formatter correspondant (ex: JsonFormatter<BackupLogEntry>)
    private readonly Dictionary<Type, object> _formatters;

    // Verrou pour la thread-safety lors des écritures concurrentes
    protected readonly object _lock = new object();

    protected LoggerBase(string outputPath, LogFormat format = LogFormat.JSON)
    {
        _outputPath = outputPath;
        _format = format;
        _formatters = new Dictionary<Type, object>();

        // Garantir que le répertoire existe
        EnsureDirectoryExists(_outputPath);
    }

    /// <summary>
    /// Enregistre un formatter personnalisé pour un type spécifique
    /// Exemple: RegisterFormatter(new CustomJsonFormatter<BackupLogEntry>())
    /// </summary>
    public void RegisterFormatter<T>(ILogFormatter<T> formatter) where T : class
    {
        Type type = typeof(T);
        _formatters[type] = formatter;
    }

    /// <summary>
    /// Récupère le formatter pour le type T
    /// Si aucun formatter personnalisé n'existe, crée un formatter par défaut
    /// </summary>
    protected ILogFormatter<T> GetFormatter<T>() where T : class
    {
        Type type = typeof(T);

        // Si un formatter personnalisé existe, l'utiliser
        if (_formatters.ContainsKey(type))
        {
            return (ILogFormatter<T>)_formatters[type];
        }

        // Sinon, créer un formatter par défaut selon le format actuel
        ILogFormatter<T> defaultFormatter = _format switch
        {
            LogFormat.JSON => new JsonFormatter<T>(prettyPrint: true, paginate: true),
            LogFormat.XML => new XmlFormatter<T>(indent: true),
            _ => throw new NotSupportedException($"Format {_format} not supported")
        };

        // Enregistrer pour réutilisation
        _formatters[type] = defaultFormatter;
        return defaultFormatter;
    }

    /// <summary>
    /// Enregistre un objet dans le log
    /// LOGIQUE:
    /// 1. Récupère le formatter pour le type T
    /// 2. Convertit l'objet en string avec le formatter
    /// 3. Lit le contenu existant du fichier
    /// 4. Ajoute la nouvelle entrée
    /// 5. Écrit tout dans le fichier
    /// </summary>
    public void Log<T>(T data) where T : class
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        lock (_lock) // Thread-safety
        {
            try
            {
                // 1. Récupérer le formatter approprié
                var formatter = GetFormatter<T>();

                // 2. Lire les données existantes
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
                        // Si parsing échoue, on part d'une liste vide
                        existingData = new List<T>();
                    }
                }

                // 3. Ajouter la nouvelle entrée
                existingData.Add(data);

                // 4. Formatter toute la collection
                string formattedContent = formatter.FormatCollection(existingData);

                // 5. Écrire dans le fichier
                WriteToFile(formattedContent, _outputPath);
            }
            catch (Exception ex)
            {
                throw new LoggerException($"Error logging data of type {typeof(T).Name}", ex);
            }
        }
    }

    /// <summary>
    /// Enregistre une collection d'objets
    /// Plus efficace que d'appeler Log() plusieurs fois
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

                // Lire les données existantes
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

                // Ajouter toutes les nouvelles entrées
                existingData.AddRange(data);

                // Formatter et écrire
                string formattedContent = formatter.FormatCollection(existingData);
                WriteToFile(formattedContent, _outputPath);
            }
            catch (Exception ex)
            {
                throw new LoggerException($"Error logging collection of type {typeof(T).Name}", ex);
            }
        }
    }

    public void SetOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        _outputPath = path;
        EnsureDirectoryExists(_outputPath);
    }

    public void SetFormat(LogFormat format)
    {
        _format = format;
        // Effacer les formatters en cache pour forcer la recréation avec le nouveau format
        _formatters.Clear();
    }

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

    public virtual void Flush()
    {
        // Implémentation par défaut : rien à faire
        // Les classes dérivées avec buffer peuvent override
    }

    /// <summary>
    /// Méthode abstraite : les classes dérivées définissent comment écrire physiquement
    /// </summary>
    protected abstract void WriteToFile(string content, string path);

    /// <summary>
    /// Méthode abstraite : les classes dérivées définissent comment lire physiquement
    /// </summary>
    protected abstract string ReadFromFile(string path);

    protected void EnsureDirectoryExists(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

}