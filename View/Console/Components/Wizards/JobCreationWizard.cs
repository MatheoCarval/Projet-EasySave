using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;
using Models.Enums;

namespace EasySave.View.Console.Components.Wizards;

/// <summary>
/// Job creation wizard - 5-step process to create a new backup job
/// Steps: 1) Name, 2) Sources, 3) Destination, 4) Backup Type, 5) Validation
/// </summary>
public class JobCreationWizard
{
    private const int MAX_SOURCES = 5;
    
    private readonly LocalizationService _localizationService;
    private readonly BackupManager _backupManager;
    
    private FrameView? _frame;
    private Action? _onComplete;
    
    // Wizard state
    private string _jobName = "";
    private List<string> _sources = new();
    private string _destination = "";
    private BackupType _backupType;

    public JobCreationWizard(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Starts the job creation wizard
    /// </summary>
    /// <param name="frame">Content frame to display in</param>
    /// <param name="onComplete">Callback when wizard completes or is cancelled</param>
    public void Start(FrameView frame, Action onComplete)
    {
        _frame = frame;
        _onComplete = onComplete;
        
        // Check if max jobs limit reached
        var jobs = _backupManager.GetAllJobs();
        if (jobs.Count >= 5)
        {
            MessageBox.ErrorQuery(T("error"), 
                string.Format(T("error_max_jobs_reached"), 5), 
                T("ok"));
            onComplete();
            return;
        }
        
        // Reset wizard state
        _jobName = "";
        _sources = new List<string>();
        _destination = "";
        
        // Start wizard
        ShowStepName();
    }

    // ==================== STEP 1: JOB NAME ====================
    
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
            var existingJob = _backupManager.GetJob(name);
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

    // ==================== STEP 2: SOURCES ====================
    
    private void ShowStepSources()
    {
        AddSourceForm();
    }

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
            var source = sourceField.Text.ToString()?.Trim() ?? "";
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

    // ==================== STEP 3: DESTINATION ====================
    
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
            var dest = destField.Text.ToString()?.Trim() ?? "";
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

    // ==================== STEP 4: BACKUP TYPE ====================
    
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

    // ==================== STEP 5: VALIDATION ====================
    
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

    // ==================== JOB CREATION ====================
    
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

    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}