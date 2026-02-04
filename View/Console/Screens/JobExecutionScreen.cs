using System;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Job execution screen - Handles execution of backup jobs
/// </summary>
public class JobExecutionScreen
{
    private readonly LocalizationService _localizationService;
    private readonly BackupManager _backupManager;

    public JobExecutionScreen(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Shows job selection screen for executing a single job
    /// </summary>
    /// <param name="frame">Content frame to display in</param>
    /// <param name="onComplete">Callback when operation completes</param>
    public void ShowExecuteOne(FrameView frame, Action onComplete)
    {
        var jobs = _backupManager.GetAllJobs();
        
        if (jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            onComplete();
            return;
        }

        frame.Title = T("execute_task_title");
        frame.RemoveAll();

        // Label
        var label = new Label(T("choose_task_to_execute"))
        {
            X = Pos.Center() - 20,
            Y = Pos.Center() - 5
        };

        // Job list
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

        // Execute button
        var executeBtn = new Button(T("execute"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        // Cancel button
        var cancelBtn = new Button(T("cancel"))
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        // Button actions
        executeBtn.Clicked += () =>
        {
            var selectedJob = jobs[listView.SelectedItem];
            ExecuteJob(selectedJob.Name, onComplete);
        };

        cancelBtn.Clicked += onComplete;

        frame.Add(label, listView, executeBtn, cancelBtn);
    }

    /// <summary>
    /// Executes a specific backup job
    /// </summary>
    /// <param name="jobName">Name of the job to execute</param>
    /// <param name="onComplete">Callback when execution completes</param>
    private void ExecuteJob(string jobName, Action onComplete)
    {
        try
        {
            // Execute backup job through BackupManager
            // This will:
            // 1. Call FileTransferService to copy files
            // 2. Write logs to logs/YYYY-MM-DD.json
            // 3. Update state.json in real-time
            _backupManager.ExecuteJob(jobName);
            
            MessageBox.Query(50, 7, T("success"), 
                T("task_executed", jobName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), 
                T("ok"));
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(T("error"), 
                $"{T("error_executing_task")}: {ex.Message}", 
                T("ok"));
        }
        finally
        {
            onComplete();
        }
    }

    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}