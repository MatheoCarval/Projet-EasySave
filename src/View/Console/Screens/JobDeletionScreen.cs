using System;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Screen for selecting and deleting backup jobs with user confirmation to prevent accidental data loss.
/// </summary>
public class JobDeletionScreen
{
    /// <summary>
    /// Service for retrieving localized text strings based on the current language setting.
    /// </summary>
    private readonly LocalizationService _localizationService;
    /// <summary>
    /// Manager for accessing and managing backup job data and operations.
    /// </summary>
    private readonly BackupManager _backupManager;

    /// <summary>
    /// Initializes a new instance of the JobDeletionScreen with required services for localization and backup management.
    /// </summary>
    public JobDeletionScreen(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Displays a list of all available backup jobs in a frame, allowing the user to select a job for deletion with confirmation. If no jobs are available, shows an error message and invokes the completion callback.
    /// </summary>
    public void Show(FrameView frame, Action onComplete)
    {
        var jobs = _backupManager.GetAllJobs();

        if (jobs.Count == 0)
        {
            MessageBox.ErrorQuery(T("error"), T("error_no_tasks_available"), T("ok"));
            onComplete();
            return;
        }

        frame.Title = T("delete_task_title");
        frame.RemoveAll();

        var label = new Label(T("choose_task_to_delete"))
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
            var selectedJob = jobs[listView.SelectedItem];

            if (ConfirmDeletion(selectedJob.Name))
            {
                DeleteJob(selectedJob.Id, onComplete);
            }
        };

        cancelBtn.Clicked += onComplete;

        frame.Add(label, listView, deleteBtn, cancelBtn);
    }

    /// <summary>
    /// Displays a confirmation dialog prompting the user to confirm the deletion of the specified backup job, returning true if confirmed.
    /// </summary>
    private bool ConfirmDeletion(string jobName)
    {
        var result = MessageBox.Query(60, 10, T("delete_confirmation"),
            T("delete_confirmation_message", jobName),
            T("yes_delete"), T("no_cancel"));

        return result == 0;
    }

    /// <summary>
    /// Deletes the backup job with the specified name, displaying a success message upon completion or an error message if deletion fails, and invoking the completion callback in all cases.
    /// </summary>
    private void DeleteJob(string jobName, Action onComplete)
    {
        try
        {
            _backupManager.DeleteJob(jobName);
            MessageBox.Query(50, 7, T("success"), T("task_deleted"), T("ok"));
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(T("error"), ex.Message, T("ok"));
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