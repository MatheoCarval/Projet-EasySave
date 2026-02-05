using System;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Job deletion screen - Handles deletion of backup jobs with confirmation
/// </summary>
public class JobDeletionScreen
{
    private readonly LocalizationService _localizationService;
    private readonly BackupManager _backupManager;

    public JobDeletionScreen(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Shows job selection screen for deletion
    /// </summary>
    /// <param name="frame">Content frame to display in</param>
    /// <param name="onComplete">Callback when operation completes</param>
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

        // Label
        var label = new Label(T("choose_task_to_delete"))
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

        // Delete button
        var deleteBtn = new Button(T("delete"))
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
        deleteBtn.Clicked += () =>
        {
            var selectedJob = jobs[listView.SelectedItem];

            // Confirmation dialog
            if (ConfirmDeletion(selectedJob.Name))
            {
                DeleteJob(selectedJob.Id, onComplete);
            }
        };

        cancelBtn.Clicked += onComplete;

        frame.Add(label, listView, deleteBtn, cancelBtn);
    }

    /// <summary>
    /// Shows confirmation dialog before deletion
    /// </summary>
    /// <param name="jobName">Name of job to delete</param>
    /// <returns>True if user confirms deletion</returns>
    private bool ConfirmDeletion(string jobName)
    {
        var result = MessageBox.Query(60, 10, T("delete_confirmation"),
            T("delete_confirmation_message", jobName),
            T("yes_delete"), T("no_cancel"));

        return result == 0;
    }

    /// <summary>
    /// Deletes a backup job
    /// </summary>
    /// <param name="jobName">Name of job to delete</param>
    /// <param name="onComplete">Callback when deletion completes</param>
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

    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}