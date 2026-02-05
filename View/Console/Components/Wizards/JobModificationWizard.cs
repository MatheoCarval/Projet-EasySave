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
/// Job modification wizard - Allows modifying existing backup jobs
/// Supports modifying: Name, Sources, Destination, Backup Type
/// </summary>
public class JobModificationWizard
{
    private const int MAX_SOURCES = 5;

    private readonly LocalizationService _localizationService;
    private readonly BackupManager _backupManager;

    private FrameView? _frame;
    private Action? _onComplete;

    // Current job being modified
    private BackupJob? _currentJob;

    public JobModificationWizard(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Starts the modification wizard
    /// </summary>
    /// <param name="frame">Content frame to display in</param>
    /// <param name="onComplete">Callback when wizard completes or is cancelled</param>
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

    // ==================== JOB SELECTION ====================

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

    // ==================== MODIFICATION OPTIONS ====================

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

    // ==================== MODIFY NAME ====================

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

    // ==================== MODIFY SOURCES ====================

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
            var newSource = sourceField.Text.ToString()?.Trim() ?? "";
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
            var newSource = sourceField.Text.ToString()?.Trim() ?? "";
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

    // ==================== MODIFY DESTINATION ====================

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
            var newDestination = destField.Text.ToString()?.Trim() ?? "";
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

    // ==================== MODIFY BACKUP TYPE ====================

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

    // ==================== HELPERS ====================

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

    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}