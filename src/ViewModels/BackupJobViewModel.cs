using Models;
using Models.Enums;
using System;
using System.IO;

namespace EasySave.ViewModels;

/// <summary>
/// ViewModel wrapper for BackupJob to provide display-friendly properties
/// </summary>
public class BackupJobViewModel : ViewModelBase
{
    private readonly BackupJob _backupJob;
    private bool _isSelected;
    private int _orderIndex;

    public BackupJobViewModel(BackupJob backupJob)
    {
        _backupJob = backupJob ?? throw new ArgumentNullException(nameof(backupJob));
    }

    public string Id => _backupJob.Id;

    public int OrderIndex
    {
        get => _orderIndex;
        set => SetProperty(ref _orderIndex, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Name
    {
        get => _backupJob.Name;
        set
        {
            if (_backupJob.Name != value)
            {
                _backupJob.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string SourcePathDisplay => _backupJob.SourcePath.Count > 0
        ? string.Join(", ", _backupJob.SourcePath)
        : "No source";

    public System.Collections.Generic.List<string> SourcePaths => _backupJob.SourcePath;

    public string TargetPath
    {
        get => _backupJob.TargetPath;
        set
        {
            if (_backupJob.TargetPath != value)
            {
                _backupJob.TargetPath = value;
                OnPropertyChanged();
            }
        }
    }

    public BackupType BackupType
    {
        get => _backupJob.BackupType;
        set
        {
            if (_backupJob.BackupType != value)
            {
                _backupJob.BackupType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackupTypeDisplay));
            }
        }
    }

    public string BackupTypeDisplay => _backupJob.BackupType.ToString();

    public BackupState BackupState
    {
        get => _backupJob.BackupState;
        set
        {
            if (_backupJob.BackupState != value)
            {
                _backupJob.BackupState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackupStateDisplay));
                OnPropertyChanged(nameof(StateColor));
            }
        }
    }

    public string BackupStateDisplay => _backupJob.BackupState.ToString();

    public string StateColor => _backupJob.BackupState switch
    {
        BackupState.ACTIVE => "#4CAF50",
        BackupState.PAUSED => "#FFA726",
        BackupState.COMPLETED => "#2196F3",
        BackupState.ERROR => "#F44336",
        BackupState.PENDING => "#9E9E9E",
        _ => "#9E9E9E"
    };

    public DateTime LastExecution => _backupJob.LastExecution;

    public string LastExecutionDisplay
    {
        get
        {
            if (_backupJob.LastExecution == DateTime.MinValue)
                return T("gui_never");
            return FormatRelativeTime(_backupJob.LastExecution);
        }
    }

    /// <summary>Target drive free space display (e.g. "12.3 GB free")</summary>
    public string DiskSpaceDisplay
    {
        get
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_backupJob.TargetPath)) return string.Empty;
                var root = Path.GetPathRoot(_backupJob.TargetPath);
                if (string.IsNullOrEmpty(root)) return string.Empty;
                var drive = new DriveInfo(root);
                if (!drive.IsReady) return string.Empty;
                return $"{FormatBytes(drive.AvailableFreeSpace)} {T("gui_free")}";
            }
            catch { return string.Empty; }
        }
    }

    public bool HasDiskSpace => !string.IsNullOrEmpty(DiskSpaceDisplay);

    public string? ErrorReason => _backupJob.ErrorReason;

    public bool HasError => _backupJob.BackupState == BackupState.ERROR && !string.IsNullOrEmpty(_backupJob.ErrorReason);
    public bool IsPaused => _backupJob.BackupState == BackupState.PAUSED;

    public string ErrorReasonDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(_backupJob.ErrorReason)) return string.Empty;
            try
            {
                var key = _backupJob.ErrorReason switch
                {
                    "error_path_not_found" => "gui_error_path_not_found",
                    "error_access_denied" => "gui_error_access_denied",
                    "error_io" => "gui_error_io",
                    _ => "gui_execution_error"
                };
                return View.GUI.App.LocalizationService?.GetTextTranslated(key) ?? _backupJob.ErrorReason;
            }
            catch { return _backupJob.ErrorReason; }
        }
    }

    public float Progress => _backupJob.Progress;

    public string ProgressDisplay => $"{_backupJob.Progress:F0}%";

    public BackupJob GetBackupJob() => _backupJob;

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(SourcePathDisplay));
        OnPropertyChanged(nameof(TargetPath));
        OnPropertyChanged(nameof(BackupType));
        OnPropertyChanged(nameof(BackupTypeDisplay));
        OnPropertyChanged(nameof(BackupState));
        OnPropertyChanged(nameof(BackupStateDisplay));
        OnPropertyChanged(nameof(StateColor));
        OnPropertyChanged(nameof(LastExecution));
        OnPropertyChanged(nameof(LastExecutionDisplay));
        OnPropertyChanged(nameof(ErrorReason));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(ErrorReasonDisplay));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ProgressDisplay));
        OnPropertyChanged(nameof(DiskSpaceDisplay));
        OnPropertyChanged(nameof(HasDiskSpace));
    }

    private static string T(string key)
    {
        try { return View.GUI.App.LocalizationService?.GetTextTranslated(key) ?? key; }
        catch { return key; }
    }

    private static string FormatRelativeTime(DateTime dt)
    {
        var span = DateTime.Now - dt;
        if (span.TotalSeconds < 60) return T("gui_time_just_now");
        if (span.TotalMinutes < 60)
            return string.Format(T("gui_time_minutes_ago"), (int)span.TotalMinutes);
        if (span.TotalHours < 24)
            return string.Format(T("gui_time_hours_ago"), (int)span.TotalHours);
        if (span.TotalDays < 7)
            return string.Format(T("gui_time_days_ago"), (int)span.TotalDays);
        if (span.TotalDays < 30)
            return string.Format(T("gui_time_weeks_ago"), (int)(span.TotalDays / 7));
        return dt.ToString("g");
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double d = bytes;
        while (d >= 1024 && i < suffixes.Length - 1) { d /= 1024; i++; }
        return $"{d:F1} {suffixes[i]}";
    }
}
