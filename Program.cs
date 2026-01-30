using EasySave.View.Console; 

namespace EasySave;

/// Point d'entrée principal de l'application EasySave

internal class Program
{

    /// Point d'entrée de l'application

    private static void Main(string[] args)
    {
        InitializeServices();
        HandleCommandLineArgs(args);
    }

    /// Initialise les services de l'application
    private static void InitializeServices()
    {
        // TODO: Initialiser BackupManager
        // TODO: Initialiser LocalizationService
        // TODO: Initialiser ConfigurationManager
    }

    /// Traite les arguments de ligne de commande

    private static void HandleCommandLineArgs(string[] args)
    {
        // Lance l'interface console
        var consoleUI = new ConsoleUI();
        consoleUI.Start();
    }
}