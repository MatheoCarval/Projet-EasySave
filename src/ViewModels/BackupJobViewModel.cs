using Models;
using Models.Enums;
using System;

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

    public string LastExecutionDisplay => _backupJob.LastExecution == DateTime.MinValue
        ? "Never"
        : _backupJob.LastExecution.ToString("g");

    public string? ErrorReason => _backupJob.ErrorReason;

    public bool HasError => _backupJob.BackupState == BackupState.ERROR && !string.IsNullOrEmpty(_backupJob.ErrorReason);

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
        OnPropertyChanged(nameof(ErrorReasonDisplay));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ProgressDisplay));
    }
}
