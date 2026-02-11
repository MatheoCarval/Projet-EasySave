using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;
using Models.Enums;

namespace EasySave.View.Console.Components.Wizards;

/// <summary>
/// Wizard component that guides users through a five-step process to create new backup jobs: collecting job name, source paths, destination path, backup type selection, and final validation before creation.
/// </summary>
public class JobCreationWizard
{
    /// <summary>
    /// Maximum number of source paths that can be added to a single backup job.
    /// </summary>
    private const int MAX_SOURCES = 5;

    /// <summary>
    /// Service for retrieving localized text strings based on the current language setting.
    /// </summary>
    private readonly LocalizationService _localizationService;
    /// <summary>
    /// Manager for creating and managing backup jobs during wizard completion.
    /// </summary>
    private readonly BackupManager _backupManager;

    /// <summary>
    /// Reference to the frame view where wizard steps are displayed.
    /// </summary>
    private FrameView? _frame;
    /// <summary>
    /// Callback action invoked when wizard completes or is cancelled by the user.
    /// </summary>
    private Action? _onComplete;

    /// <summary>
    /// Accumulated job name provided by the user during wizard execution.
    /// </summary>
    private string _jobName = "";
    /// <summary>
    /// Accumulated list of source paths provided by the user during wizard execution.
    /// </summary>
    private List<string> _sources = new();
    /// <summary>
    /// Accumulated destination path provided by the user during wizard execution.
    /// </summary>
    private string _destination = "";
    /// <summary>
    /// Accumulated backup type selection (COMPLETE or DIFFERENTIAL) during wizard execution.
    /// </summary>
    private BackupType _backupType;

    /// <summary>
    /// Initializes a new instance of the JobCreationWizard with required services for localization and backup management.
    /// </summary>
    public JobCreationWizard(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Initiates the job creation wizard, validating that the maximum job limit has not been reached, resetting wizard state, and starting the first step to collect job name information.
    /// </summary>
    public void Start(FrameView frame, Action onComplete)
    {
        _frame = frame;
        _onComplete = onComplete;

        _jobName = "";
        _sources = new List<string>();
        _destination = "";

        ShowStepName();
    }

    /// <summary>
    /// Displays the first wizard step where the user enters a backup job name, validates input, and checks for name uniqueness before proceeding.
    /// </summary>
    private void ShowStepName()
    {
        _frame!.Title = T("create_task_step_name");
        _frame!.RemoveAll();

        var label = new Label(T("task_name_label"))
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
            var name = nameField.Text.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.ErrorQuery(T("error"), T("error_task_name_required"), T("ok"));
                return;
            }

            // Check if name already exists
            var existingJob = _backupManager.GetJobByName(name);
            if (existingJob != null)
            {
                MessageBox.ErrorQuery(T("error"),
                    T("error_task_name_exists", name),
                    T("ok"));
                return;
            }

            _jobName = name;
            ShowStepSources();
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, nameField, nextBtn, cancelBtn);
    }

    /// <summary>
    /// Initiates the second wizard step for collecting source directory paths from the user.
    /// </summary>
    private void ShowStepSources()
    {
        AddSourceForm();
    }

    /// <summary>
    /// Displays the form for adding a single source path, allowing users to add multiple sources up to the maximum limit with option to skip or cancel at each step.
    /// </summary>
    private void AddSourceForm()
    {
        _frame!.Title = T("create_task_step_sources", _sources.Count + 1, MAX_SOURCES);
        _frame!.RemoveAll();

        var label = new Label(T("source_label", _sources.Count + 1, MAX_SOURCES))
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

        var skipBtn = new Button(_sources.Count > 0 ? T("skip") : T("cancel"))
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
            var source = SanitizePath(sourceField.Text.ToString());
            if (string.IsNullOrEmpty(source))
            {
                MessageBox.ErrorQuery(T("error"), T("error_source_empty"), T("ok"));
                return;
            }

            _sources.Add(source);

            if (_sources.Count < MAX_SOURCES)
            {
                int result = MessageBox.Query(50, 7, T("add"),
                    T("add_another_source"),
                    T("yes"), T("no"));

                if (result == 0) // Yes
                {
                    AddSourceForm();
                }
                else // No
                {
                    ShowStepDestination();
                }
            }
            else
            {
                MessageBox.Query(50, 7, T("info"),
                    T("source_limit_reached", MAX_SOURCES),
                    T("ok"));
                ShowStepDestination();
            }
        };

        skipBtn.Clicked += () =>
        {
            if (_sources.Count == 0)
            {
                MessageBox.ErrorQuery(T("error"), T("error_at_least_one_source"), T("ok"));
                return;
            }
            ShowStepDestination();
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, sourceField, addBtn, skipBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the third wizard step where the user enters the destination path for backup files, with format guidance and validation before proceeding.
    /// </summary>
    private void ShowStepDestination()
    {
        _frame!.Title = T("create_task_step_destination");
        _frame!.RemoveAll();

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
            var dest = SanitizePath(destField.Text.ToString());
            if (string.IsNullOrEmpty(dest))
            {
                MessageBox.ErrorQuery(T("error"), T("error_destination_required"), T("ok"));
                return;
            }

            _destination = dest;
            ShowStepBackupType();
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, destField, infoLabel, nextBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the fourth wizard step where the user selects between COMPLETE and DIFFERENTIAL backup types before proceeding to validation.
    /// </summary>
    private void ShowStepBackupType()
    {
        _frame!.Title = T("create_task_step_backup_type");
        _frame!.RemoveAll();

        var label = new Label(T("backup_type_label"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 4
        };

        var backupTypes = new List<string>
        {
            T("backup_type_full"),
            T("backup_type_differential")
        };

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
            _backupType = listView.SelectedItem == 0 ? BackupType.COMPLETE : BackupType.DIFFERENTIAL;
            ShowStepValidation();
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the fifth and final wizard step showing a summary of all collected job information for user review and confirmation before creation.
    /// </summary>
    private void ShowStepValidation()
    {
        var sourcesText = string.Join("\n", _sources.Select((s, i) => $"  [{i + 1}/{_sources.Count}] {s}"));

        var summary = T("summary_creation",
            _jobName,
            sourcesText,
            _destination,
            _backupType == BackupType.COMPLETE ? T("backup_type_full") : T("backup_type_differential"));

        var result = MessageBox.Query(60, 18, T("validation"), summary, T("validate"), T("cancel"));

        if (result == 0) // Validate
        {
            CreateJob();
        }
        else // Cancel
        {
            _onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Creates the backup job using the accumulated wizard data via the BackupManager, displaying success or error messages accordingly.
    /// </summary>
    private void CreateJob()
    {
        try
        {
            // Create job via BackupManager
            _backupManager.CreateJob(_jobName, _sources, _destination, _backupType);

            MessageBox.Query(50, 7, T("success"), T("task_created"), T("ok"));
            _onComplete?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(T("error"),
                $"{T("error_creating_task")}: {ex.Message}",
                T("ok"));
            _onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Retrieves the localized text for the specified key, optionally formatting it with the provided arguments.
    /// </summary>
    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }

    private static string SanitizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Trim('"');
    }
}