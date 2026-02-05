using System;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Screen for selecting and executing individual backup jobs with real-time progress tracking and status reporting.
/// </summary>
public class JobExecutionScreen
{
    /// <summary>
    /// Service for retrieving localized text strings based on the current language setting.
    /// </summary>
    private readonly LocalizationService _localizationService;
    /// <summary>
    /// Manager for accessing and managing backup job data and execution operations.
    /// </summary>
    private readonly BackupManager _backupManager;

    /// <summary>
    /// Initializes a new instance of the JobExecutionScreen with required services for localization and backup management.
    /// </summary>
    public JobExecutionScreen(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Displays a list of all available backup jobs in a frame, allowing the user to select a single job for execution. If no jobs are available, shows an error message and invokes the completion callback.
    /// </summary>
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

        var label = new Label(T("choose_task_to_execute"))
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
            var selectedJob = jobs[listView.SelectedItem];
            ExecuteJob(selectedJob.Id, onComplete);
        };

        cancelBtn.Clicked += onComplete;

        frame.Add(label, listView, executeBtn, cancelBtn);
    }

    /// <summary>
    /// Executes the specified backup job through the BackupManager, which handles file transfer, logging, and state persistence, displaying success or error messages accordingly.
    /// </summary>
    private void ExecuteJob(string jobId, Action onComplete)
    {
        try
        {
            _backupManager.ExecuteJob(jobId);

            MessageBox.Query(50, 7, T("success"),
                T("task_executed", jobId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
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

    /// <summary>
    /// Retrieves the localized text for the specified key, optionally formatting it with the provided arguments.
    /// </summary>
    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}