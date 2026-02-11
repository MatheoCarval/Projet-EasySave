using EasySave.Services;
using EasySave.Services.Managers;
using EasySave.View.Console;
using Services.Managers;
using EasyLog.Abstractions;
using EasyLog.Loggers;
using EasyLog.Enums;
using Services.Writers;
using Models.Enums;

namespace EasySave;

/// <summary>
/// Main entry point for the EasySave application, responsible for initializing services and managing backup operations.
/// </summary>
public class Program
{
    /// <summary>
    /// Provides localization support for the application.
    /// </summary>
    private static LocalizationService? _localizationService;
    /// <summary>
    /// Manages backup job execution and coordination.
    /// </summary>
    private static BackupManager? _backupManager;
    /// <summary>
    /// Manages application configuration including language and log format settings.
    /// </summary>
    private static ConfigurationManager? _configurationManager;

    /// <summary>
    /// Entry point of the application that initializes services and processes command line arguments.
    /// </summary>
    private static void Main(string[] args)
    {
        try
        {
            // Set up global exception handler to log crashes
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
                    Directory.CreateDirectory(logDir);
                    File.AppendAllText(Path.Combine(logDir, "crash.log"),
                        $"[{DateTime.Now}] {ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}\n\n");
                }
                catch { }
            };

            // Initialize services
            InitializeServices();

            // Check for theme argument (CLI overrides config)
            bool? darkModeOverride = null;
            var remainingArgs = new System.Collections.Generic.List<string>();
            foreach (var arg in args)
            {
                if (arg.Equals("--light", StringComparison.OrdinalIgnoreCase))
                    darkModeOverride = false;
                else if (arg.Equals("--dark", StringComparison.OrdinalIgnoreCase))
                    darkModeOverride = true;
                else
                    remainingArgs.Add(arg);
            }

            // Use CLI override if provided, otherwise load from saved config
            bool darkMode = darkModeOverride ?? _configurationManager!.LoadConfiguration().GetDarkMode();

            // If no remaining arguments, launch UI
            if (remainingArgs.Count == 0)
            {
                LaunchUI(darkMode);
                return;
            }

            // Parse and execute jobs by index
            var indices = ParseJobIndices(remainingArgs[0]);
            ExecuteJobsByIndices(indices);
        }
        catch (Exception ex)
        {
            // Log to file for WinExe scenarios where console isn't visible
            try
            {
                var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(Path.Combine(logDir, "error.log"),
                    $"[{DateTime.Now}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{T("error")}: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Initializes all application services including localization, state writer, and backup manager with file transfer service and logging.
    /// </summary>
    private static void InitializeServices()
    {
        _configurationManager = ConfigurationManager.GetInstance();
        var config = _configurationManager.LoadConfiguration();

        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave"
        );

        Directory.CreateDirectory(appData);

        var language = NormalizeLanguage(config.GetLanguage());
        _localizationService = new LocalizationService(language);

        string logPath = config.GetLogFilePath();
        if (string.IsNullOrWhiteSpace(logPath))
        {
            logPath = Path.Combine(appData, "logs.json");
        }

        string statePath = config.GetStateFilePath();
        if (string.IsNullOrWhiteSpace(statePath) ||
            Path.GetFileName(statePath).Equals("Config.json", StringComparison.OrdinalIgnoreCase))
        {
            statePath = Path.Combine(appData, "state.json");
        }

        EnsureDirectoryForFile(logPath);
        EnsureDirectoryForFile(statePath);

        ILogger logger = config.GetLogFormat() == LogFormat.XML
            ? new XmlLogger(logPath)
            : new JsonLogger(logPath);

        var stateWriter = new StateWriter(statePath);
        var fileTransferService = new FileTransferService(logger, stateWriter);

        _backupManager = new BackupManager(fileTransferService, stateWriter, config.GetBlockedApplications());
    }

    /// <summary>
    /// Parses a job selection pattern and returns indices from a pattern like "1-3" or "1;3".
    /// </summary>
    private static List<int> ParseJobIndices(string arg)
    {
        var indices = new List<int>();

        if (arg.Contains('-'))
        {
            var parts = arg.Split('-');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int start) &&
                int.TryParse(parts[1], out int end))
            {
                if (start > end)
                    throw new ArgumentException($"Invalid range: {arg}");

                for (int i = start; i <= end; i++)
                {
                    indices.Add(i);
                }
            }
            else
            {
                throw new ArgumentException($"Invalid range format: {arg}");
            }
        }
        // Handle list with semicolons (e.g., "1;3;5")
        else if (arg.Contains(';'))
        {
            var parts = arg.Split(';');
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int index))
                {
                    indices.Add(index);
                }
                else
                {
                    throw new ArgumentException($"Invalid index: {part}");
                }
            }
        }
        // Handle single index (e.g., "2")
        else
        {
            if (int.TryParse(arg, out int index))
            {
                indices.Add(index);
            }
            else
            {
                throw new ArgumentException($"Invalid index: {arg}");
            }
        }

        return indices;
    }

    /// <summary>
    /// Executes backup jobs selected by their indices.
    /// </summary>
    private static void ExecuteJobsByIndices(List<int> indices)
    {
        var allJobs = _backupManager!.GetAllJobs();

        if (allJobs.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{T("error_no_tasks_available")}");
            Console.ResetColor();
            Environment.Exit(1);
            return;
        }

        // List available jobs
        Console.WriteLine($"{allJobs.Count} {T("menu_display_tasks").ToLower()}");
        for (int i = 0; i < allJobs.Count; i++)
        {
            Console.WriteLine($"   [{i + 1}] {allJobs[i].Name}");
        }
        Console.WriteLine();

        var errors = new List<string>();

        foreach (var index in indices)
        {
            // Convert 1-based to 0-based
            int arrayIndex = index - 1;

            if (arrayIndex < 0 || arrayIndex >= allJobs.Count)
            {
                errors.Add($"{T("error")}: Index {index} (1-{allJobs.Count})");
                continue;
            }

            var job = allJobs[arrayIndex];

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{T("menu_execute_task")} [{index}]: {job.Name}");
                Console.ResetColor();
                Console.WriteLine($"   {T("source_label", "", "").Replace("{0}/{1}:", "")}: {string.Join(", ", job.SourcePath)}");
                Console.WriteLine($"   {T("destination_label")}: {job.TargetPath}");
                Console.WriteLine($"   {T("backup_type_label")}: {GetBackupTypeDisplay(job.BackupType)}");

                _backupManager.ExecuteJob(job.Id);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{T("success")} [{index}] '{job.Name}'");
                Console.ResetColor();
                Console.WriteLine($"   {allJobs.Count} files");
                Console.WriteLine($"   {job.Progress}%");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                errors.Add($"[{index}] {job.Name}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{T("error")} [{index}] {job.Name}: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        // Summary
        int successCount = indices.Count - errors.Count;
        Console.WriteLine("═══════════════════════════════════");
        Console.WriteLine($"{T("information")}: {successCount}/{indices.Count} {T("success").ToLower()}");

        if (errors.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n{errors.Count} {T("error").ToLower()}(s):");
            foreach (var error in errors)
            {
                Console.WriteLine($"  • {error}");
            }
            Console.ResetColor();
            Environment.Exit(1);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{T("executing_all_tasks").Split('\n')[2]}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Launches the interactive GUI for managing backups.
    /// </summary>
    /// <param name="darkMode">True for dark theme, false for light theme</param>
    private static void LaunchUI(bool darkMode = true)
    {
        //var consoleUI = new ConsoleUI(_localizationService!, _backupManager!);
        //consoleUI.Start();
        EasySave.View.GUI.GUILauncher.Launch(_localizationService!, _backupManager!, darkMode);
    }

    /// <summary>
    /// Normalizes language code to lowercase and removes regional suffix.
    /// </summary>
    private static string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        var trimmed = language.Trim();
        var normalized = trimmed.Contains('-')
            ? trimmed.Split('-')[0]
            : trimmed;

        return normalized.ToLowerInvariant();
    }

    private static void EnsureDirectoryForFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Retrieves and formats the localized text for the specified translation key with optional format arguments.
    /// </summary>
    private static string T(string key, params object[] args)
    {
        var text = _localizationService!.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }

    /// <summary>
    /// Returns the localized display name for a backup type.
    /// </summary>
    private static string GetBackupTypeDisplay(BackupType type)
    {
        return type switch
        {
            BackupType.COMPLETE => T("backup_type_full"),
            BackupType.DIFFERENTIAL => T("backup_type_differential"),
            _ => type.ToString()
        };
    }
}