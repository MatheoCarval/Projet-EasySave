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
        var consoleUI = new ConsoleUI(_localizationService!, _backupManager!);
        consoleUI.Start();
    }
}