using EasySave.Services;
using EasySave.View.Console;
using Services.Managers;
using EasyLog.Loggers;
using Services.Writers;
using Models.Enums;

namespace EasySave;

/// <summary>
/// Point d'entrée principal de l'application EasySave
/// Supporte l'exécution par index: EasySave.exe 1-3 ou EasySave.exe 1;3
/// </summary>
internal class Program
{
    private static LocalizationService? _localizationService;
    private static BackupManager? _backupManager;

    private static void Main(string[] args)
    {
        try
        {
            // Initialize services
            InitializeServices();

            // If no arguments, launch UI
            if (args.Length == 0)
            {
                LaunchUI();
                return;
            }

            // Parse and execute jobs by index
            var indices = ParseJobIndices(args[0]);
            ExecuteJobsByIndices(indices);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{T("error")}: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    private static void InitializeServices()
    {
        _localizationService = new LocalizationService("en");

        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave"
        );

        Directory.CreateDirectory(Path.Combine(appData, "logs"));

        var logger = new JsonLogger("logs.json");
        var stateWriter = new StateWriter("state.json");
        var fileTransferService = new FileTransferService(logger, stateWriter);

        _backupManager = new BackupManager(fileTransferService, stateWriter, maxJobs: 5);
    }

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

    private static void LaunchUI()
    {
        var consoleUI = new ConsoleUI(_localizationService!, _backupManager!);
        consoleUI.Start();
    }

    private static string T(string key, params object[] args)
    {
        var text = _localizationService!.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }

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