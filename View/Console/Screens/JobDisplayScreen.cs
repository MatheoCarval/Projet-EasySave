using System;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;
using Models;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Screen for displaying a list of all backup jobs with the ability to view detailed information for each job.
/// </summary>
public class JobDisplayScreen
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
    /// Initializes a new instance of the JobDisplayScreen with required services for localization and backup management.
    /// </summary>
    public JobDisplayScreen(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Displays a list of all available backup jobs in a frame, allowing the user to select and view job details. If no jobs are available, shows an error message and invokes the completion callback.
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

        frame.Title = T("display_tasks_title");
        frame.RemoveAll();

        var label = new Label(T("choose_task_for_details"))
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

        var detailBtn = new Button(T("details"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        var backBtn = new Button(T("back"))
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        detailBtn.Clicked += () =>
        {
            var selectedJob = jobs[listView.SelectedItem];
            ShowJobDetails(selectedJob);
            Show(frame, onComplete);
        };

        backBtn.Clicked += onComplete;

        frame.Add(label, listView, detailBtn, backBtn);
    }

    /// <summary>
    /// Displays detailed information about the specified backup job in a modal message box.
    /// </summary>
    private void ShowJobDetails(BackupJob job)
    {
        var details = FormatJobDetails(job);
        MessageBox.Query(60, 18, T("details"), details, T("ok"));
    }

    /// <summary>
    /// Formats all job information including name, sources, destination, backup type, state, and progress into a formatted localized string for display.
    /// </summary>
    private string FormatJobDetails(BackupJob job)
    {
        var sourcesText = string.Join("\n", job.SourcePath.Select((s, i) => $"  [{i + 1}] {s}"));

        return T("task_details",
            job.Name,
            job.Name,
            sourcesText,
            job.TargetPath,
            job.BackupType.ToString(),
            job.BackupState.ToString(),
            job.Progress,
            job.TotalFiles,
            FormatBytes(job.TotalSize)
        );
    }

    /// <summary>
    /// Converts a byte count into a human-readable string with appropriate unit (B, KB, MB, GB, or TB).
    /// </summary>
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}