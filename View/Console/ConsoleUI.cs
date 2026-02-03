using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;

namespace EasySave.View.Console;

/// <summary>
/// ConsoleUI class - Manages the terminal user interface for EasySave backup application
/// </summary>
internal class ConsoleUI
{
    /// <summary>Maximum number of backup jobs allowed</summary>
    private const int MAX_JOBS = 5;

    /// <summary>Maximum number of sources per backup job</summary>
    private const int MAX_SOURCES = 5;

    /// <summary>List of all backup jobs with their properties: id, name, sources, destinations, backup type</summary>
    private List<(int id, string name, List<string> sources, List<string> destinations, string backupType)> _jobs = new();

    /// <summary>Counter for generating unique job IDs</summary>
    private int _nextJobId = 1;

    /// <summary>Main application window</summary>
    private Window? _mainWindow;

    /// <summary>Content frame for displaying different screens</summary>
    private FrameView? _contentFrame;

    /// <summary>Localization service for translations</summary>
    private LocalizationService _localizationService;

    // TODO: _backupManager: BackupManager
    // TODO: _configManager: ConfigurationManager

    /// <summary>
    /// Initializes ConsoleUI with a localization service
    /// </summary>
    /// <param name="localizationService">The localization service instance</param>
    public ConsoleUI(LocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <summary>
    /// Initializes and runs the terminal UI application
    /// </summary>
    public void Start()
    {
        Application.Init();
        var top = Application.Top;

        _mainWindow = CreateMainWindow();
        top.Add(_mainWindow);

        Application.Run(top);
        Application.Shutdown();
    }

    private Window CreateMainWindow()
    {
        var window = new Window(_localizationService.GetTextTranslated("app_title"))
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var menuBar = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_Menu", new MenuItem[]
            {
                new MenuItem(_localizationService.GetTextTranslated("menu_quit"), "", () => Application.RequestStop())
            })
        });
        window.Add(menuBar);

        _contentFrame = new FrameView(_localizationService.GetTextTranslated("main_menu_title"))
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var mainMenu = CreateMenuListView();
        _contentFrame!.Add(mainMenu);
        window.Add(_contentFrame);

        return window;
    }

    private ListView CreateMenuListView()
    {
        var items = new List<string>
        {
            _localizationService.GetTextTranslated("menu_create_task"),
            _localizationService.GetTextTranslated("menu_modify_task"),
            _localizationService.GetTextTranslated("menu_delete_task"),
            _localizationService.GetTextTranslated("menu_execute_task"),
            _localizationService.GetTextTranslated("menu_execute_all_tasks"),
            _localizationService.GetTextTranslated("menu_display_tasks"),
            _localizationService.GetTextTranslated("menu_change_settings"),
            _localizationService.GetTextTranslated("menu_quit_main")
        };

        var listView = new ListView(items)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 4,
            Width = 30,
            Height = 8,
            AllowsMarking = false,
            CanFocus = true
        };

        listView.OpenSelectedItem += (e) =>
        {
            HandleMenuSelection(listView.SelectedItem);
        };

        return listView;
    }

    private void HandleMenuSelection(int selected)
    {
        // Handle main menu selection and route to appropriate function
        switch (selected)
        {
            case 0:
                CreateNewJob();
                break;
            case 1:
                ModifyJob();
                break;
            case 2:
                DeleteJob();
                break;
            case 3:
                ExecuteJob();
                break;
            case 4:
                ExecuteAllJobs();
                break;
            case 5:
                DisplayJobs();
                break;
            case 6:
                ChangeSettings();
                break;
            case 7:
                Application.RequestStop();
                break;
        }
    }

    /// <summary>
    /// Creates a new backup job through a multi-step wizard
    /// Steps: 1) Job Name, 2) Sources, 3) Destination, 4) Backup Type, 5) Validation
    /// </summary>
    private void CreateNewJob()
    {
        if (_jobs.Count >= MAX_JOBS)
        {
            MessageBox.ErrorQuery(_localizationService.GetTextTranslated("error"), 
                string.Format(_localizationService.GetTextTranslated("error_max_jobs_reached"), MAX_JOBS), 
                _localizationService.GetTextTranslated("ok"));
            return;
        }

        var taskName = "";
        var sources = new List<string>();
        var destination = "";
        var backupType = "";

        // Étape 1 : Nom
        CreateJobStepName((name) =>
        {
            taskName = name;
            // Étape 2 : Sources
            CreateJobStepSources((sourcesResult) =>
            {
                sources = sourcesResult;
                // Étape 3 : Destination
                CreateJobStepDestination((dest) =>
                {
                    destination = dest;
                    // Étape 4 : Type de sauvegarde
                    CreateJobStepBackupType((type) =>
                    {
                        backupType = type;
                        // Étape 5 : Validation
                        CreateJobStepValidation(taskName, sources, destination, backupType);
                    });
                });
            });
        });
    }

    /// <summary>
    /// Step 1: Prompts user to enter the job name
    /// </summary>
    private void CreateJobStepName(Action<string> onComplete)
    {
        _contentFrame!.Title = _localizationService.GetTextTranslated("create_task_step_name");
        _contentFrame!.RemoveAll();

        var label = new Label(_localizationService.GetTextTranslated("task_name_label"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var nameField = new TextField("")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var nextBtn = new Button(T("next"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        nextBtn.Clicked += () =>
        {
            var taskName = nameField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(taskName))
            {
                MessageBox.ErrorQuery(_localizationService.GetTextTranslated("error"), 
                    _localizationService.GetTextTranslated("error_task_name_required"), 
                    _localizationService.GetTextTranslated("ok"));
                return;
            }
            onComplete(taskName);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, nameField, nextBtn, cancelBtn);
    }

    /// <summary>
    /// Step 2: Allows user to add source directories (up to MAX_SOURCES)
    /// </summary>
    private void CreateJobStepSources(Action<List<string>> onComplete)
    {
        var sources = new List<string>();
        AddSourceForm(sources, onComplete);
    }

    /// <summary>
    /// Recursive form for adding or modifying multiple sources
    /// Displays "Source X/Y" format to show progress
    /// </summary>
    private void AddSourceForm(List<string> sources, Action<List<string>> onComplete)
    {
        _contentFrame!.Title = T("create_task_step_sources", sources.Count + 1, MAX_SOURCES);
        _contentFrame!.RemoveAll();

        var label = new Label(T("source_label", sources.Count + 1, MAX_SOURCES))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var sourceField = new TextField("")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var addBtn = new Button(T("add"))
        {
            X = Pos.Center() - 25,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var skipBtn = new Button(T("skip"))
        {
            X = Pos.Center() - 5,
            Y = Pos.Center() + 2
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 15,
            Y = Pos.Center() + 2
        };

        addBtn.Clicked += () =>
        {
            var source = sourceField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(source))
            {
                MessageBox.ErrorQuery(T("error"), T("error_source_empty"), T("ok"));
                return;
            }

            sources.Add(source);

            if (sources.Count < MAX_SOURCES)
            {
                int result = MessageBox.Query(50, 7, T("add"), T("add_another_source"), T("yes"), T("no"));
                if (result == 0)
                {
                    AddSourceForm(sources, onComplete);
                }
                else
                {
                    onComplete(sources);
                }
            }
            else
            {
                MessageBox.Query(50, 7, T("source_limit_reached"), T("source_limit_reached", MAX_SOURCES), T("ok"));
                onComplete(sources);
            }
        };

        skipBtn.Clicked += () =>
        {
            if (sources.Count == 0)
            {
                MessageBox.ErrorQuery(T("error"), T("error_at_least_one_source"), T("ok"));
                return;
            }
            onComplete(sources);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, sourceField, addBtn, skipBtn, cancelBtn);
    }

    /// <summary>
    /// Step 3: Prompts user to enter the backup destination path
    /// </summary>
    private void CreateJobStepDestination(Action<string> onComplete)
    {
        _contentFrame!.Title = T("create_task_step_destination");
        _contentFrame!.RemoveAll();

        var label = new Label(T("destination_label"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var destField = new TextField("")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var infoLabel = new Label(T("destination_format"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center()
        };

        var nextBtn = new Button(T("next"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        nextBtn.Clicked += () =>
        {
            var destination = destField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(destination))
            {
                MessageBox.ErrorQuery(T("error"), T("error_destination_required"), T("ok"));
                return;
            }
            onComplete(destination);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, destField, infoLabel, nextBtn, cancelBtn);
    }

    /// <summary>
    /// Step 4: Allows user to select backup type (Full or Differential)
    /// </summary>
    private void CreateJobStepBackupType(Action<string> onComplete)
    {
        _contentFrame!.Title = T("create_task_step_backup_type");
        _contentFrame!.RemoveAll();

        var label = new Label(T("backup_type_label"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 4
        };

        var backupTypes = new List<string> { T("backup_type_full"), T("backup_type_differential") };
        var listView = new ListView(backupTypes)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 2,
            Width = 30,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        selectBtn.Clicked += () =>
        {
            var selectedType = backupTypes[listView.SelectedItem];
            onComplete(selectedType);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Step 5: Shows a summary of the job and asks for confirmation before saving
    /// </summary>
    private void CreateJobStepValidation(string taskName, List<string> sources, string destination, string backupType)
    {
        _contentFrame!.Title = T("create_task_validation");
        _contentFrame!.RemoveAll();

        var sourcesText = "";
        for (int i = 0; i < sources.Count; i++)
        {
            sourcesText += $"  [{i + 1}/{sources.Count}] {sources[i]}\n";
        }

        var summary = T("summary_creation", taskName, sourcesText, destination, backupType);

        var result = MessageBox.Query(60, 18, T("validation"), summary, T("validate"), T("cancel"));

        if (result == 0)
        {
            var destinations = new List<string> { destination };
            _jobs.Add((_nextJobId++, taskName, sources, destinations, backupType));
            MessageBox.Query(50, 7, T("success"), T("task_created"), T("ok"));
            // TODO: Appeler BackupManager.CreateJob(taskName, sources, destination, backupType)
            DisplayMainMenu();
        }
        else
        {
            DisplayMainMenu();
        }
    }

    /// <summary>
    /// Executes a single selected backup job
    /// </summary>
    private void ExecuteJob()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            return;
        }

        var jobNames = new List<string>();
        foreach (var job in _jobs)
        {
            jobNames.Add(job.name);
        }

        _contentFrame!.Title = T("execute_task_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("choose_task_to_execute"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(jobNames)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, jobNames.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var executeBtn = new Button(T("execute"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        executeBtn.Clicked += () =>
        {
            var selectedJob = _jobs[listView.SelectedItem];
            MessageBox.Query(50, 7, T("execute"), T("task_executed", selectedJob.name, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), T("ok"));
            // TODO: Appeler BackupManager.ExecuteJob(selectedJob.id)
            DisplayMainMenu();
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, executeBtn, cancelBtn);
    }

    /// <summary>
    /// Executes all backup jobs in the system
    /// </summary>
    private void ExecuteAllJobs()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            return;
        }

        MessageBox.Query(50, 7, T("execute_all_tasks_title"), T("executing_all_tasks", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), T("ok"));
        // TODO: Appeler BackupManager.ExecuteAll()
        DisplayMainMenu();
    }

    /// <summary>
    /// Modifies an existing backup job
    /// Allows user to select which field to modify (name, sources, destination, or backup type)
    /// </summary>
    private void ModifyJob()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            return;
        }

        var jobNames = new List<string>();
        foreach (var job in _jobs)
        {
            jobNames.Add(job.name);
        }

        _contentFrame!.Title = T("modify_task_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("choose_task_to_modify"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(jobNames)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, jobNames.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var modifyBtn = new Button(T("modify"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        modifyBtn.Clicked += () =>
        {
            var selectedIndex = listView.SelectedItem;
            var selectedJob = _jobs[selectedIndex];
            ShowModifyOptions(selectedIndex, selectedJob);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, modifyBtn, cancelBtn);
    }

    /// <summary>
    /// Displays menu for selecting which job parameter to modify
    /// </summary>
    private void ShowModifyOptions(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = T("modify_choose_parameter");
        _contentFrame!.RemoveAll();

        var label = new Label(T("what_to_modify"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var options = new List<string> { T("modify_task_name"), T("modify_sources"), T("modify_destination"), T("modify_backup_type") };
        var listView = new ListView(options)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 1,
            Width = 30,
            Height = 5,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 5
        };

        selectBtn.Clicked += () =>
        {
            switch (listView.SelectedItem)
            {
                case 0:
                    ModifyJobName(jobIndex, job);
                    break;
                case 1:
                    ModifyJobSources(jobIndex, job);
                    break;
                case 2:
                    ModifyJobDestination(jobIndex, job);
                    break;
                case 3:
                    ModifyJobBackupType(jobIndex, job);
                    break;
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Modifies the job name with confirmation before saving
    /// </summary>
    private void ModifyJobName(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = T("modify_name_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("new_name"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var nameField = new TextField(job.name)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var confirmBtn = new Button(T("confirm"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        confirmBtn.Clicked += () =>
        {
            var newName = nameField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.ErrorQuery(T("error"), T("error_field_empty", T("modify_task_name")), T("ok"));
                return;
            }

            if (newName != job.name)
            {
                ShowModifyConfirmation(jobIndex, job, T("modify_task_name"), job.name, newName, () =>
                {
                    _jobs[jobIndex] = (job.id, newName, job.sources, job.destinations, job.backupType);
                    MessageBox.Query(50, 7, T("success"), T("task_modified"), T("ok"));
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("error_no_modification"), T("ok"));
                AskContinueModifying(jobIndex);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, nameField, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Displays options to modify existing sources or add new ones
    /// </summary>
    private void ModifyJobSources(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = T("modify_sources_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("what_to_do"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var options = new List<string> { T("modify_existing_source"), T("add_new_source") };
        var listView = new ListView(options)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 1,
            Width = 40,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 3,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 3
        };

        selectBtn.Clicked += () =>
        {
            if (listView.SelectedItem == 0)
            {
                ShowEditSourcesList(jobIndex, job);
            }
            else
            {
                ShowAddNewSourcesForm(jobIndex, job);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Shows list of existing sources for the user to select one to edit
    /// </summary>
    private void ShowEditSourcesList(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = T("modify_sources_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("choose_source_to_modify"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(job.sources)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, job.sources.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var editBtn = new Button(T("modify"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        editBtn.Clicked += () =>
        {
            var sourceIndex = listView.SelectedItem;
            ShowEditSourceForm(jobIndex, job, sourceIndex);
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, editBtn, cancelBtn);
    }

    /// <summary>
    /// Allows editing a specific source at the given index
    /// </summary>
    private void ShowEditSourceForm(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job, int sourceIndex)
    {
        _contentFrame!.Title = T("create_task_step_sources", sourceIndex + 1, job.sources.Count);
        _contentFrame!.RemoveAll();

        var label = new Label(T("new_value"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var sourceField = new TextField(job.sources[sourceIndex])
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var confirmBtn = new Button(T("confirm"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        confirmBtn.Clicked += () =>
        {
            var newSource = sourceField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(newSource))
            {
                MessageBox.ErrorQuery(T("error"), T("error_source_empty"), T("ok"));
                return;
            }

            if (newSource != job.sources[sourceIndex])
            {
                ShowModifyConfirmation(jobIndex, job, T("modify_sources", sourceIndex + 1), job.sources[sourceIndex], newSource, () =>
                {
                    var updatedSources = new List<string>(job.sources);
                    updatedSources[sourceIndex] = newSource;
                    _jobs[jobIndex] = (job.id, job.name, updatedSources, job.destinations, job.backupType);
                    MessageBox.Query(50, 7, T("success"), T("source_modified"), T("ok"));
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("error_no_modification"), T("ok"));
                AskContinueModifying(jobIndex);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, sourceField, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Form for adding new sources to an existing job
    /// </summary>
    private void ShowAddNewSourcesForm(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        var newSources = new List<string>(job.sources);
        AddModifySourceForm(newSources, job, jobIndex, () =>
        {
            if (newSources.Count > job.sources.Count)
            {
                ShowModifyConfirmation(jobIndex, job, T("modify_sources"), string.Join(", ", job.sources), string.Join(", ", newSources), () =>
                {
                    _jobs[jobIndex] = (job.id, job.name, newSources, job.destinations, job.backupType);
                    MessageBox.Query(50, 7, T("success"), T("sources_modified"), T("ok"));
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("no_new_source_added"), T("ok"));
                AskContinueModifying(jobIndex);
            }
        });
    }

    /// <summary>
    /// Recursive form for adding or modifying sources during job editing
    /// </summary>
    private void AddModifySourceForm(List<string> sources, (int id, string name, List<string> sources, List<string> destinations, string backupType) job, int jobIndex, Action onComplete)
    {
        _contentFrame!.Title = T("create_task_step_sources", sources.Count + 1, MAX_SOURCES);
        _contentFrame!.RemoveAll();

        var label = new Label(T("source_label", sources.Count + 1, MAX_SOURCES))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var sourceField = new TextField(sources.Count < job.sources.Count ? sources[sources.Count] : "")
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var addBtn = new Button(T("add"))
        {
            X = Pos.Center() - 25,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var skipBtn = new Button(T("validate"))
        {
            X = Pos.Center() - 5,
            Y = Pos.Center() + 2
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 15,
            Y = Pos.Center() + 2
        };

        addBtn.Clicked += () =>
        {
            var source = sourceField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(source))
            {
                MessageBox.ErrorQuery(T("error"), T("error_source_empty"), T("ok"));
                return;
            }

            if (sources.Count < job.sources.Count)
            {
                sources[sources.Count] = source;
            }
            else
            {
                sources.Add(source);
            }

            if (sources.Count < MAX_SOURCES)
            {
                int result = MessageBox.Query(50, 7, T("add"), T("add_another_source"), T("yes"), T("no"));
                if (result == 0)
                {
                    AddModifySourceForm(sources, job, jobIndex, onComplete);
                }
                else
                {
                    onComplete();
                }
            }
            else
            {
                MessageBox.Query(50, 7, T("source_limit_reached"), T("source_limit_reached", MAX_SOURCES), T("ok"));
                onComplete();
            }
        };

        skipBtn.Clicked += () =>
        {
            if (sources.Count == 0)
            {
                MessageBox.ErrorQuery(T("error"), T("error_at_least_one_source"), T("ok"));
                return;
            }
            onComplete();
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, sourceField, addBtn, skipBtn, cancelBtn);
    }

    /// <summary>
    /// Modifies the job destination with confirmation before saving
    /// </summary>
    private void ModifyJobDestination(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = T("modify_destination_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("new_destination"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var destField = new TextField(job.destinations[0])
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 2,
            Width = 40,
            Height = 1
        };

        var infoLabel = new Label(T("destination_format"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center()
        };

        var confirmBtn = new Button("Confirmer")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        confirmBtn.Clicked += () =>
        {
            var newDestination = destField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(newDestination))
            {
                MessageBox.ErrorQuery(T("error"), T("error_destination_required"), T("ok"));
                return;
            }

            if (newDestination != job.destinations[0])
            {
                ShowModifyConfirmation(jobIndex, job, T("modify_destination"), job.destinations[0], newDestination, () =>
                {
                    var newDestinations = new List<string> { newDestination };
                    _jobs[jobIndex] = (job.id, job.name, job.sources, newDestinations, job.backupType);
                    MessageBox.Query(50, 7, T("success"), T("task_modified"), T("ok"));
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("error_no_modification"), T("ok"));
                AskContinueModifying(jobIndex);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, destField, infoLabel, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Modifies the backup type (Full or Differential) with confirmation before saving
    /// </summary>
    private void ModifyJobBackupType(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job)
    {
        _contentFrame!.Title = T("modify_backup_type_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("new_backup_type"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 4
        };

        var backupTypes = new List<string> { T("backup_type_full"), T("backup_type_differential") };
        var selectedIndex = backupTypes.IndexOf(job.backupType);
        if (selectedIndex < 0) selectedIndex = 0;

        var listView = new ListView(backupTypes)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 2,
            Width = 30,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true,
            SelectedItem = selectedIndex
        };

        var confirmBtn = new Button("Confirmer")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button("Annuler")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        confirmBtn.Clicked += () =>
        {
            var newType = backupTypes[listView.SelectedItem];

            if (newType != job.backupType)
            {
                ShowModifyConfirmation(jobIndex, job, T("modify_backup_type"), job.backupType, newType, () =>
                {
                    _jobs[jobIndex] = (job.id, job.name, job.sources, job.destinations, newType);
                    MessageBox.Query(50, 7, T("success"), T("task_modified"), T("ok"));
                    AskContinueModifying(jobIndex);
                });
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("error_no_modification"), T("ok"));
                AskContinueModifying(jobIndex);
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Prompts user to continue modifying other fields or return to main menu
    /// </summary>
    private void AskContinueModifying(int jobIndex)
    {
        var updatedJob = _jobs[jobIndex];
        var result = MessageBox.Query(50, 7, T("continue"), T("continue_modifying"), T("yes"), T("no"));

        if (result == 0)
        {
            ShowModifyOptions(jobIndex, updatedJob);
        }
        else
        {
            DisplayMainMenu();
        }
    }

    /// <summary>
    /// Shows a confirmation dialog with old and new values before applying changes
    /// </summary>
    private void ShowModifyConfirmation(int jobIndex, (int id, string name, List<string> sources, List<string> destinations, string backupType) job, string parameterName, string oldValue, string newValue, Action onConfirm)
    {
        var summary = T("modification_confirmation", job.name, parameterName, oldValue, newValue);

        var result = MessageBox.Query(70, 20, T("confirmation"), summary, T("yes_save"), T("no_cancel"));

        if (result == 0)
        {
            onConfirm();
        }
        else
        {
            DisplayMainMenu();
        }
    }

    /// <summary>
    /// Deletes a selected backup job from the system
    /// </summary>
    private void DeleteJob()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            return;
        }

        var jobNames = new List<string>();
        foreach (var job in _jobs)
        {
            jobNames.Add(job.name);
        }

        _contentFrame!.Title = T("delete_task_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("choose_task_to_delete"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(jobNames)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, jobNames.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var deleteBtn = new Button(T("delete"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        deleteBtn.Clicked += () =>
        {
            var selectedIndex = listView.SelectedItem;
            var selectedJob = _jobs[selectedIndex];

            // Ask for confirmation before deleting
            var result = MessageBox.Query(60, 10, T("delete_confirmation"),
                T("delete_confirmation_message", selectedJob.name),
                T("yes_delete"), T("no_cancel"));

            if (result == 0)
            {
                _jobs.RemoveAt(selectedIndex);
                MessageBox.Query(50, 7, T("success"), T("task_deleted"), T("ok"));
                // TODO: Appeler BackupManager.DeleteJob(selectedJob.id)
                DisplayMainMenu();
            }
            else
            {
                DisplayMainMenu();
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, deleteBtn, cancelBtn);
    }

    /// <summary>
    /// Displays detailed information about all backup jobs
    /// </summary>
    private void DisplayJobs()
    {
        if (_jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            return;
        }

        var jobNames = new List<string>();
        foreach (var job in _jobs)
        {
            jobNames.Add(job.name);
        }

        _contentFrame!.Title = T("display_tasks_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("choose_task_for_details"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(jobNames)
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, jobNames.Count + 1),
            AllowsMarking = false,
            CanFocus = true
        };

        var detailBtn = new Button(T("details"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var cancelBtn = new Button(T("back"))
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        detailBtn.Clicked += () =>
        {
            var selectedJob = _jobs[listView.SelectedItem];
            var sourcesText = "";
            for (int i = 0; i < selectedJob.sources.Count; i++)
            {
                sourcesText += $"  [{i + 1}/{selectedJob.sources.Count}] {selectedJob.sources[i]}\n";
            }
            var destText = "";
            for (int i = 0; i < selectedJob.destinations.Count; i++)
            {
                destText += $"  [{i + 1}] {selectedJob.destinations[i]}\n";
            }
            var details = T("task_details", selectedJob.id, selectedJob.name, sourcesText, destText, selectedJob.backupType);
            MessageBox.Query(60, 18, T("details"), details, T("ok"));
            DisplayJobs();
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, detailBtn, cancelBtn);
    }

    /// <summary>
    /// Displays application settings menu for configuring language and log format
    /// </summary>
    private void ChangeSettings()
    {
        _contentFrame!.Title = T("change_settings_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("choose_parameter"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var options = new List<string> { "Choisir la langue", "Choisir le format de log" };
        var listView = new ListView(options)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 1,
            Width = 30,
            Height = 4,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button("Sélectionner")
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var cancelBtn = new Button("Retour")
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        selectBtn.Clicked += () =>
        {
            switch (listView.SelectedItem)
            {
                case 0:
                    ChooseLanguage();
                    break;
                case 1:
                    ChooseLogFormat();
                    break;
                case 2:
                    DisplayMainMenu();
                    break;
            }
        };

        cancelBtn.Clicked += () =>
        {
            DisplayMainMenu();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Allows user to select the application language
    /// </summary>
    private void ChooseLanguage()
    {
        _contentFrame!.Title = T("choose_language_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("select_language"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var languages = new List<string> { T("language_french"), T("language_english") };
        var languageCodes = new List<string> { "fr", "en" };
        var listView = new ListView(languages)
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() - 1,
            Width = 20,
            Height = 4,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var cancelBtn = new Button(T("back"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        selectBtn.Clicked += () =>
        {
            var selectedLanguageCode = languageCodes[listView.SelectedItem];
            _localizationService.ChangeLanguage(selectedLanguageCode);
            MessageBox.Query(50, 7, T("success"), T("language_changed"), T("ok"));
            // Refresh the UI to reflect new language
            DisplayMainMenu();
        };

        cancelBtn.Clicked += () =>
        {
            ChangeSettings();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Allows user to select the log file format (JSON or XML)
    /// </summary>
    private void ChooseLogFormat()
    {
        _contentFrame!.Title = T("choose_log_format_title");
        _contentFrame!.RemoveAll();

        var label = new Label(T("select_format"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var formats = new List<string> { "JSON", "XML" };
        var listView = new ListView(formats)
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() - 1,
            Width = 20,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true
        };

        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var cancelBtn = new Button(T("back"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        selectBtn.Clicked += () =>
        {
            // TODO: Appeler ConfigurationManager.UpdateLogFormat(listView.SelectedItem)
            MessageBox.Query(50, 7, T("success"), T("log_format_changed"), T("ok"));
            ChangeSettings();
        };

        cancelBtn.Clicked += () =>
        {
            ChangeSettings();
        };

        _contentFrame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Returns to the main menu screen
    /// </summary>
    private void DisplayMainMenu()
    {
        _contentFrame!.Title = _localizationService.GetTextTranslated("main_menu_title");
        _contentFrame!.RemoveAll();
        _contentFrame!.Add(CreateMenuListView());
    }

    /// <summary>
    /// Helper method to get translation with formatting
    /// </summary>
    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}
