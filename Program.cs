using EasySave.Services;
using EasySave.View.Console;
using Services.Managers;
using EasyLog.Abstractions;
using EasyLog.Loggers;
using Services;
using Services.Writers;

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
    /// Entry point of the application that initializes services and processes command line arguments.
    /// </summary>
    private static void Main(string[] args)
    {
        InitializeServices();
        HandleCommandLineArgs(args);
    }

    /// <summary>
    /// Initializes all application services including localization, state writer, and backup manager with file transfer service and logging.
    /// </summary>
    private static void InitializeServices()
    {
        _localizationService = new LocalizationService("fr");



        StateWriter stateWriter = new StateWriter("state.json");



        _backupManager = new BackupManager(
            new FileTransferService(
                new JsonLogger("logs.json"),
                stateWriter
            ),
            stateWriter
        );

    }

    /// <summary>
    /// Processes command line arguments to show jobs list, execute specific jobs by pattern, or start interactive console mode.
    /// </summary>
    private static void HandleCommandLineArgs(string[] args)
    {
        if (args.Length > 0 && (args[0] == "-s" || args[0] == "-show"))
        {
            ShowJobsList();
            return;
        }

        if (args.Length > 0 && !args[0].StartsWith("-"))
        {
            ExecuteJobsByPattern(args[0]);
            return;
        }

        var consoleUI = new ConsoleUI(_localizationService!, _backupManager!);
        consoleUI.Start();
    }

    /// <summary>
    /// Displays all available backup jobs with their details including name, type, source, destination, and current state.
    /// </summary>
    private static void ShowJobsList()
    {
        var jobs = _backupManager!.GetAllJobs();

        if (jobs.Count == 0)
        {
            Console.WriteLine("Aucune sauvegarde disponible.");
            return;
        }

        Console.WriteLine("=== Liste des sauvegardes ===\n");
        for (int i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];
            Console.WriteLine($"{i + 1}. {job.Name}");
            Console.WriteLine($"   Type: {job.BackupType}");
            Console.WriteLine($"   Source: {string.Join(", ", job.SourcePath)}");
            Console.WriteLine($"   Destination: {job.TargetPath}");
            Console.WriteLine($"   État: {job.BackupState}\n");
        }
    }

    /// <summary>
    /// Executes backup jobs selected by a pattern string supporting range (1-3), semicolon-separated (1;3), or single number (1) formats.
    /// </summary>
    private static void ExecuteJobsByPattern(string pattern)
    {
        var jobs = _backupManager!.GetAllJobs();

        if (jobs.Count == 0)
        {
            Console.WriteLine("Aucune sauvegarde disponible.");
            return;
        }

        var indicesToExecute = ParseJobPattern(pattern, jobs.Count);

        if (indicesToExecute.Count == 0)
        {
            Console.WriteLine($"Aucun index valide trouvé dans le pattern: {pattern}");
            return;
        }

        Console.WriteLine($"Exécution de {indicesToExecute.Count} sauvegarde(s)...\n");

        foreach (var index in indicesToExecute)
        {
            try
            {
                var job = jobs[index];
                Console.WriteLine($"[{index + 1}/{indicesToExecute.Count}] Exécution de '{job.Name}'...");
                _backupManager.ExecuteJob(job.Id);
                Console.WriteLine($"✓ '{job.Name}' complétée avec succès.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Erreur lors de l'exécution: {ex.Message}\n");
            }
        }
    }

    /// <summary>
    /// Parses a job selection pattern and returns a sorted list of zero-based indices matching the pattern format (range, semicolon-separated, or single number).
    /// </summary>
    private static List<int> ParseJobPattern(string pattern, int totalJobs)
    {
        var indices = new SortedSet<int>();

        if (pattern.Contains("-"))
        {
            var parts = pattern.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
            {
                start = Math.Max(1, start);
                end = Math.Min(totalJobs, end);

                if (start <= end)
                {
                    for (int i = start; i <= end; i++)
                    {
                        indices.Add(i - 1);
                    }
                }
            }
        }
        else if (pattern.Contains(";"))
        {
            var parts = pattern.Split(';');
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int jobNum) && jobNum >= 1 && jobNum <= totalJobs)
                {
                    indices.Add(jobNum - 1);
                }
            }
        }
        else if (int.TryParse(pattern, out int jobNum) && jobNum >= 1 && jobNum <= totalJobs)
        {
            indices.Add(jobNum - 1);
        }

        return indices.ToList();
    }
}