using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;
using Models;
using Models.Enums;

namespace EasySave.View.Console.Components.Wizards;

/// <summary>
/// Provides an interactive wizard for modifying existing backup jobs, allowing changes to job name, source directories, destination path, and backup type.
/// </summary>
public class JobModificationWizard
{
    /// <summary>
    /// Maximum number of source directories allowed per backup job.
    /// </summary>
    private const int MAX_SOURCES = 5;

    /// <summary>
    /// Localization service for retrieving translated text for UI elements.
    /// </summary>
    private readonly LocalizationService _localizationService;
    /// <summary>
    /// Backup manager instance for retrieving, saving, and managing backup job data.
    /// </summary>
    private readonly BackupManager _backupManager;

    /// <summary>
    /// Frame view container for displaying wizard UI components.
    /// </summary>
    private FrameView? _frame;
    /// <summary>
    /// Callback action invoked when the wizard completes or is cancelled.
    /// </summary>
    private Action? _onComplete;

    /// <summary>
    /// Currently selected backup job being modified.
    /// </summary>
    private BackupJob? _currentJob;

    /// <summary>
    /// Initializes JobModificationWizard with required dependencies for localization and backup job management.
    /// </summary>
    public JobModificationWizard(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Initiates the modification wizard, displaying the job selection screen and handling the modification workflow.
    /// </summary>
    public void Start(FrameView frame, Action onComplete)
    {
        _frame = frame;
        _onComplete = onComplete;

        var jobs = _backupManager.GetAllJobs();

        if (jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            onComplete();
            return;
        }

        ShowJobSelection();
    }

    /// <summary>
    /// Displays the job selection screen where users can choose which backup job to modify.
    /// </summary>
    private void ShowJobSelection()
    {
        var jobs = _backupManager.GetAllJobs();

        _frame!.Title = T("modify_task_title");
        _frame!.RemoveAll();

        var label = new Label(T("choose_task_to_modify"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var jobNames = jobs.Select(j => j.Name).ToList();
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
            _currentJob = jobs[listView.SelectedItem];
            ShowModificationOptions();
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, listView, modifyBtn, cancelBtn);
    }

    /// <summary>
    /// Displays menu options allowing users to choose which job parameter to modify.
    /// </summary>
    private void ShowModificationOptions()
    {
        _frame!.Title = T("modify_choose_parameter");
        _frame!.RemoveAll();

        var label = new Label(T("what_to_modify"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var options = new List<string>
        {
            T("modify_task_name"),
            T("modify_sources"),
            T("modify_destination"),
            T("modify_backup_type")
        };

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
                case 0: ModifyName(); break;
                case 1: ModifySources(); break;
                case 2: ModifyDestination(); break;
                case 3: ModifyBackupType(); break;
            }
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the interface for modifying the backup job name with validation for duplicate names.
    /// </summary>
    private void ModifyName()
    {
        _frame!.Title = T("modify_name_title");
        _frame!.RemoveAll();

        var label = new Label(T("new_name"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var nameField = new TextField(_currentJob!.Name)
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
                MessageBox.ErrorQuery(T("error"), T("error_task_name_required"), T("ok"));
                return;
            }

            if (newName != _currentJob!.Name)
            {
                // Check if new name already exists
                var existingJob = _backupManager.GetJobByName(newName);
                if (existingJob != null)
                {
                    MessageBox.ErrorQuery(T("error"),
                        T("error_task_name_exists", newName),
                        T("ok"));
                    return;
                }

                var confirmed = ConfirmModification(
                    T("modify_task_name"),
                    _currentJob!.Name,
                    newName);

                if (confirmed)
                {
                    _currentJob!.Name = newName;
                    _backupManager.SaveJob(_currentJob!);
                    MessageBox.Query(50, 7, T("success"), T("task_modified"), T("ok"));
                    AskContinueModifying();
                }
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("error_no_modification"), T("ok"));
                AskContinueModifying();
            }
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, nameField, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Displays options for modifying source directories: editing existing sources or adding new ones.
    /// </summary>
    private void ModifySources()
    {
        _frame!.Title = T("modify_sources_title");
        _frame!.RemoveAll();

        var label = new Label(T("what_to_do"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        var options = new List<string>
        {
            T("modify_existing_source"),
            T("add_new_source")
        };

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
            if (listView.SelectedItem == 0) // Modify existing
            {
                ShowSourcesList();
            }
            else // Add new
            {
                if (_currentJob!.SourcePath.Count >= MAX_SOURCES)
                {
                    MessageBox.ErrorQuery(T("error"),
                        T("source_limit_reached", MAX_SOURCES),
                        T("ok"));
                    return;
                }
                AddNewSource();
            }
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, listView, selectBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the list of current source directories for the job, allowing users to select one to edit.
    /// </summary>
    private void ShowSourcesList()
    {
        _frame!.Title = T("modify_sources_title");
        _frame!.RemoveAll();

        var label = new Label(T("choose_source_to_modify"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        var listView = new ListView(_currentJob!.SourcePath.ToList())
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3,
            Width = 40,
            Height = Math.Min(10, _currentJob!.SourcePath.Count + 1),
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
            EditSource(listView.SelectedItem);
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, listView, editBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the interface for editing a specific source directory at the given index.
    /// </summary>
    private void EditSource(int index)
    {
        _frame!.Title = T("modify_sources_title");
        _frame!.RemoveAll();

        var label = new Label(T("new_value"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var sourceField = new TextField(_currentJob!.SourcePath[index])
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
            var newSource = SanitizePath(sourceField.Text.ToString());
            if (string.IsNullOrEmpty(newSource))
            {
                MessageBox.ErrorQuery(T("error"), T("error_source_empty"), T("ok"));
                return;
            }

            if (newSource != _currentJob!.SourcePath[index])
            {
                var confirmed = ConfirmModification(
                    T("modify_sources", index + 1),
                    _currentJob!.SourcePath[index],
                    newSource);

                if (confirmed)
                {
                    _currentJob!.SourcePath[index] = newSource;
                    _backupManager.SaveJob(_currentJob!);
                    MessageBox.Query(50, 7, T("success"), T("source_modified"), T("ok"));
                    AskContinueModifying();
                }
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("error_no_modification"), T("ok"));
                AskContinueModifying();
            }
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, sourceField, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the interface for adding a new source directory to the job, enforcing the maximum source limit.
    /// </summary>
    private void AddNewSource()
    {
        _frame!.Title = T("add_new_source");
        _frame!.RemoveAll();

        var label = new Label(T("source_label", _currentJob!.SourcePath.Count + 1, MAX_SOURCES))
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
            X = Pos.Center() - 10,
            Y = Pos.Center() + 2,
            IsDefault = true
        };

        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 2
        };

        addBtn.Clicked += () =>
        {
            var newSource = SanitizePath(sourceField.Text.ToString());
            if (string.IsNullOrEmpty(newSource))
            {
                MessageBox.ErrorQuery(T("error"), T("error_source_empty"), T("ok"));
                return;
            }

            _currentJob!.SourcePath.Add(newSource);
            _backupManager.SaveJob(_currentJob!);
            MessageBox.Query(50, 7, T("success"), T("source_added"), T("ok"));
            AskContinueModifying();
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, sourceField, addBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the interface for modifying the backup job destination path with format guidance.
    /// </summary>
    private void ModifyDestination()
    {
        _frame!.Title = T("modify_destination_title");
        _frame!.RemoveAll();

        var label = new Label(T("new_destination"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 3
        };

        var destField = new TextField(_currentJob!.TargetPath)
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
            var newDestination = SanitizePath(destField.Text.ToString());
            if (string.IsNullOrEmpty(newDestination))
            {
                MessageBox.ErrorQuery(T("error"), T("error_destination_required"), T("ok"));
                return;
            }

            if (newDestination != _currentJob!.TargetPath)
            {
                var confirmed = ConfirmModification(
                    T("modify_destination"),
                    _currentJob!.TargetPath,
                    newDestination);

                if (confirmed)
                {
                    _currentJob!.TargetPath = newDestination;
                    _backupManager.SaveJob(_currentJob!);
                    MessageBox.Query(50, 7, T("success"), T("task_modified"), T("ok"));
                    AskContinueModifying();
                }
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("error_no_modification"), T("ok"));
                AskContinueModifying();
            }
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, destField, infoLabel, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Displays the interface for changing the backup job type between complete and differential backups.
    /// </summary>
    private void ModifyBackupType()
    {
        _frame!.Title = T("modify_backup_type_title");
        _frame!.RemoveAll();

        var label = new Label(T("new_backup_type"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 4
        };

        var backupTypes = new List<string>
        {
            T("backup_type_full"),
            T("backup_type_differential")
        };

        var currentIndex = _currentJob!.BackupType == BackupType.COMPLETE ? 0 : 1;

        var listView = new ListView(backupTypes)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 2,
            Width = 30,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true,
            SelectedItem = currentIndex
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
            var newType = listView.SelectedItem == 0 ? BackupType.COMPLETE : BackupType.DIFFERENTIAL;

            if (newType != _currentJob!.BackupType)
            {
                var confirmed = ConfirmModification(
                    T("modify_backup_type"),
                    _currentJob!.BackupType.ToString(),
                    newType.ToString());

                if (confirmed)
                {
                    _currentJob!.BackupType = newType;
                    _backupManager.SaveJob(_currentJob!);
                    MessageBox.Query(50, 7, T("success"), T("task_modified"), T("ok"));
                    AskContinueModifying();
                }
            }
            else
            {
                MessageBox.Query(50, 7, T("information"), T("error_no_modification"), T("ok"));
                AskContinueModifying();
            }
        };

        cancelBtn.Clicked += _onComplete;

        _frame!.Add(label, listView, confirmBtn, cancelBtn);
    }

    /// <summary>
    /// Displays a confirmation dialog showing the parameter change details and returns true if the user confirms the modification.
    /// </summary>
    private bool ConfirmModification(string parameterName, string oldValue, string newValue)
    {
        var summary = T("modification_confirmation",
            _currentJob!.Name,
            parameterName,
            oldValue,
            newValue);

        var result = MessageBox.Query(70, 20, T("confirmation"), summary, T("yes_save"), T("no_cancel"));
        return result == 0;
    }

    /// <summary>
    /// Prompts the user to continue modifying other job parameters or exit the wizard.
    /// </summary>
    private void AskContinueModifying()
    {
        var result = MessageBox.Query(50, 7, T("continue"),
            T("continue_modifying"),
            T("yes"), T("no"));

        if (result == 0) // Yes
        {
            ShowModificationOptions();
        }
        else // No
        {
            _onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Retrieves translated text from the localization service, applying optional format arguments.
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