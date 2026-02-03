using EasySave.Services;
using EasySave.View.Console;

namespace EasySave;

/// Point d'entrée principal de l'application EasySave
internal class Program
{
    private static LocalizationService? _localizationService;

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

        // TODO: Initialiser BackupManager
        // TODO: Initialiser ConfigurationManager
    }

    /// Traite les arguments de ligne de commande et lance l'interface
    private static void HandleCommandLineArgs(string[] args)
    {
        var consoleUI = new ConsoleUI(_localizationService!);
        consoleUI.Start();
    }
}