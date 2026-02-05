using System;
using System.Linq;
using Terminal.Gui;
using EasySave.Services;
using Services.Managers;
using Models;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Job display screen - Shows list of jobs and their detailed information
/// </summary>
public class JobDisplayScreen
{
    private readonly LocalizationService _localizationService;
    private readonly BackupManager _backupManager;

    public JobDisplayScreen(LocalizationService localizationService, BackupManager backupManager)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
    }

    /// <summary>
    /// Shows list of all jobs with option to view details
    /// </summary>
    /// <param name="frame">Content frame to display in</param>
    /// <param name="onComplete">Callback when user returns to menu</param>
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

        // Label
        var label = new Label(T("choose_task_for_details"))
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

        // Details button
        var detailBtn = new Button(T("details"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() + 5,
            IsDefault = true
        };

        // Back button
        var backBtn = new Button(T("back"))
        {
            X = Pos.Center() + 5,
            Y = Pos.Center() + 5
        };

        // Button actions
        detailBtn.Clicked += () =>
        {
            var selectedJob = jobs[listView.SelectedItem];
            ShowJobDetails(selectedJob);
            // Refresh the list after viewing details
            Show(frame, onComplete);
        };

        backBtn.Clicked += onComplete;

        frame.Add(label, listView, detailBtn, backBtn);
    }

    /// <summary>
    /// Shows detailed information about a specific job in a message box
    /// </summary>
    /// <param name="job">Job to display details for</param>
    private void ShowJobDetails(BackupJob job)
    {
        var details = FormatJobDetails(job);
        MessageBox.Query(60, 18, T("details"), details, T("ok"));
    }

    /// <summary>
    /// Formats job information for display
    /// </summary>
    /// <param name="job">Job to format</param>
    /// <returns>Formatted string with all job details</returns>
    private string FormatJobDetails(BackupJob job)
    {
        var sourcesText = string.Join("\n", job.SourcePath.Select((s, i) => $"  [{i + 1}] {s}"));

        return T("task_details",
            job.Name,                           // Job name
            job.Name,                           // Job ID (same as name for now)
            sourcesText,                        // Sources list
            job.TargetPath,                     // Destination
            job.BackupType.ToString(),          // Backup type (COMPLETE/DIFFERENTIAL)
            job.BackupState.ToString(),               // Current state
            job.Progress,                       // Progress percentage
            job.TotalFiles,                     // Total files count
            FormatBytes(job.TotalSize)          // Total size formatted
        );
    }

    /// <summary>
    /// Formats bytes to human-readable format (B, KB, MB, GB)
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