using System;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;
using EasySave.View.Console.Screens;
using EasySave.View.Console.Components.Wizards;

namespace EasySave.View.Console;

/// <summary>
/// Main entry point and coordinator for the Terminal.Gui-based user interface. Manages navigation between screens and application lifecycle.
/// </summary>
public class ConsoleUI
{
    /// <summary>
    /// Service for retrieving localized text strings.
    /// </summary>
    private readonly LocalizationService _localizationService;
    /// <summary>
    /// Service for managing backup job execution and coordination.
    /// </summary>
    private readonly BackupManager _backupManager;

    /// <summary>
    /// The main application window.
    /// </summary>
    private Window? _mainWindow;
    /// <summary>
    /// The content frame that holds the current screen display.
    /// </summary>
    private FrameView? _contentFrame;

    /// <summary>
    /// Screen for displaying and managing the main menu.
    /// </summary>
    private readonly MainMenuScreen _mainMenuScreen;
    /// <summary>
    /// Screen for executing backup jobs.
    /// </summary>
    private readonly JobExecutionScreen _jobExecutionScreen;
    /// <summary>
    /// Screen for deleting backup jobs.
    /// </summary>
    private readonly JobDeletionScreen _jobDeletionScreen;
    /// <summary>
    /// Screen for displaying backup job details.
    /// </summary>
    private readonly JobDisplayScreen _jobDisplayScreen;
    /// <summary>
    /// Screen for application settings configuration.
    /// </summary>
    private readonly SettingsScreen _settingsScreen;

    /// <summary>
    /// Wizard for creating new backup jobs.
    /// </summary>
    private readonly JobCreationWizard _jobCreationWizard;
    /// <summary>
    /// Wizard for modifying existing backup jobs.
    /// </summary>
    private readonly JobModificationWizard _jobModificationWizard;

    /// <summary>
    /// Initializes the ConsoleUI with localization and backup management services, and instantiates all screens and wizards.
    /// </summary>
    public ConsoleUI(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));

        // Initialize screens
        _mainMenuScreen = new MainMenuScreen(_localizationService);
        _jobExecutionScreen = new JobExecutionScreen(_localizationService, _backupManager);
        _jobDeletionScreen = new JobDeletionScreen(_localizationService, _backupManager);
        _jobDisplayScreen = new JobDisplayScreen(_localizationService, _backupManager);
        _settingsScreen = new SettingsScreen(_localizationService);

        // Initialize wizards
        _jobCreationWizard = new JobCreationWizard(_localizationService, _backupManager);
        _jobModificationWizard = new JobModificationWizard(_localizationService, _backupManager);
    }

    /// <summary>
    /// Starts the terminal UI application
    /// </summary>
    public void Start()
    {
        Application.Init();

        try
        {
            var top = Application.Top;
            _mainWindow = CreateMainWindow();
            top.Add(_mainWindow);

            Application.Run(top);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    /// <summary>
    /// Creates the main window with menu bar and content frame
    /// </summary>
    private Window CreateMainWindow()
    {
        var window = new Window(T("app_title"))
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Menu bar
        var menuBar = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_Menu", new MenuItem[]
            {
                new MenuItem(T("menu_quit"), "", () => Application.RequestStop())
            })
        });
        window.Add(menuBar);

        // Content frame
        _contentFrame = new FrameView(T("main_menu_title"))
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        window.Add(_contentFrame);

        // Display main menu
        DisplayMainMenu();

        return window;
    }

    /// <summary>
    /// Displays the main menu screen
    /// </summary>
    public void DisplayMainMenu()
    {
        _contentFrame!.Title = T("main_menu_title");
        _contentFrame!.RemoveAll();

        var menuView = _mainMenuScreen.GetView(HandleMenuSelection);
        _contentFrame!.Add(menuView);
    }

    /// <summary>
    /// Processes menu selection input and routes to the appropriate screen or wizard (0-7 for Create, Modify, Delete, Execute One, Execute All, Display, Settings, and Quit).
    /// </summary>
    private void HandleMenuSelection(int selected)
    {
        switch (selected)
        {
            case 0: // Create job
                _jobCreationWizard.Start(_contentFrame!, () => DisplayMainMenu());
                break;

            case 1: // Modify job
                _jobModificationWizard.Start(_contentFrame!, () => DisplayMainMenu());
                break;

            case 2: // Delete job
                _jobDeletionScreen.Show(_contentFrame!, () => DisplayMainMenu());
                break;

            case 3: // Execute one job
                _jobExecutionScreen.ShowExecuteOne(_contentFrame!, () => DisplayMainMenu());
                break;

            case 4: // Execute all jobs
                ExecuteAllJobs();
                break;

            case 5: // Display jobs
                _jobDisplayScreen.Show(_contentFrame!, () => DisplayMainMenu());
                break;

            case 6: // Settings
                _settingsScreen.Show(_contentFrame!, () => DisplayMainMenu());
                break;

            case 7: // Quit
                Application.RequestStop();
                break;
        }
    }

    /// <summary>
    /// Executes all backup jobs sequentially
    /// </summary>
    private void ExecuteAllJobs()
    {
        var jobs = _backupManager.GetAllJobs();

        if (jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            return;
        }

        try
        {
            // Execute all jobs via BackupManager
            _backupManager.ExecuteAll();

            MessageBox.Query(50, 7, T("success"),
                T("executing_all_tasks", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                T("ok"));
        }
        catch (AggregateException ex)
        {
            var errors = string.Join("\n", ex.InnerExceptions.Select(e => $"- {e.Message}"));
            MessageBox.ErrorQuery(T("error"),
                $"{T("error_executing_jobs")}\n{errors}",
                T("ok"));
        }

        DisplayMainMenu();
    }

    /// <summary>
    /// Retrieves and formats a localized text string by key, optionally applying format arguments.
    /// </summary>
    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}