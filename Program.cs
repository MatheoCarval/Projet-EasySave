using EasySave.Services;
using EasySave.View.Console;
using Services.Managers;

using EasyLog.Abstractions;
using EasyLog.Loggers;
using Services;
using Services.Writers;

namespace EasySave;

/// Point d'entrée principal de l'application EasySave
internal class Program
{
    private static LocalizationService? _localizationService;
    private static BackupManager? _backupManager;
    // private static ConfigurationManager? _configurationManager;

    /// Point d'entrée de l'application
    private static void Main(string[] args)
    {
        InitializeServices();
        HandleCommandLineArgs(args);
    }

    /// Initialise les services de l'application
    private static void InitializeServices()
    {
        // Initialize LocalizationService with default language (French)
        _localizationService = new LocalizationService("fr");


    // TODO : CHANGE THE PATHS BELOW TO CONFIGURATION VALUES

        StateWriter stateWriter = new StateWriter("state.json");


        // TODO: Initialiser ConfigurationManager
        
        _backupManager = new BackupManager(
            new FileTransferService(
                new JsonLogger("logs.json"),
                stateWriter
            ),
            stateWriter
        );

    }

    /// Traite les arguments de ligne de commande et lance l'interface
    private static void HandleCommandLineArgs(string[] args)
    {
        // Afficher les jobs avec l'argument -s ou -show
        if (args.Length > 0 && (args[0] == "-s" || args[0] == "-show"))
        {
            ShowJobsList();
            return;
        }

        // Exécuter les jobs spécifiés par numéros
        if (args.Length > 0 && !args[0].StartsWith("-"))
        {
            ExecuteJobsByPattern(args[0]);
            return;
        }

        // Mode interactif par défaut
        var consoleUI = new ConsoleUI(_localizationService!, _backupManager!);
        consoleUI.Start();
    }

    /// Affiche la liste des jobs avec numérotation
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

    /// Exécute les jobs selon le pattern spécifié (1-3 ou 1;3)
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

    /// Parse le pattern de jobs (1-3 ou 1;3) et retourne les indices
    private static List<int> ParseJobPattern(string pattern, int totalJobs)
    {
        var indices = new SortedSet<int>();

        // Pattern avec tiret: 1-3
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
                        indices.Add(i - 1); // Convertir à index 0-based
                    }
                }
            }
        }
        // Pattern avec point-virgule: 1;3
        else if (pattern.Contains(";"))
        {
            var parts = pattern.Split(';');
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int jobNum) && jobNum >= 1 && jobNum <= totalJobs)
                {
                    indices.Add(jobNum - 1); // Convertir à index 0-based
                }
            }
        }
        // Simple numéro: 1
        else if (int.TryParse(pattern, out int jobNum) && jobNum >= 1 && jobNum <= totalJobs)
        {
            indices.Add(jobNum - 1); // Convertir à index 0-based
        }

        return indices.ToList();
    }
}