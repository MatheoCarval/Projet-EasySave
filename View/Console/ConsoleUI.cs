using System;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;
using EasySave.View.Console.Screens;
using EasySave.View.Console.Components.Wizards;

namespace EasySave.View.Console;

/// <summary>
/// Main ConsoleUI class - Entry point and coordinator for the terminal interface
/// Orchestrates navigation between screens and manages the application lifecycle
/// </summary>
public class ConsoleUI
{
    private readonly LocalizationService _localizationService;
    private readonly BackupManager _backupManager;
    
    private Window? _mainWindow;
    private FrameView? _contentFrame;
    
    // Screens
    private readonly MainMenuScreen _mainMenuScreen;
    private readonly JobExecutionScreen _jobExecutionScreen;
    private readonly JobDeletionScreen _jobDeletionScreen;
    private readonly JobDisplayScreen _jobDisplayScreen;
    private readonly SettingsScreen _settingsScreen;
    
    // Wizards
    private readonly JobCreationWizard _jobCreationWizard;
    private readonly JobModificationWizard _jobModificationWizard;

    /// <summary>
    /// Initializes ConsoleUI with required services
    /// </summary>
    /// <param name="localizationService">Service for translations</param>
    /// <param name="backupManager">Service for managing backup jobs</param>
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
    /// Handles menu selection and routes to appropriate screen
    /// </summary>
    /// <param name="selected">Selected menu index (0-7)</param>
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
    /// Helper method for translations
    /// </summary>
    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}