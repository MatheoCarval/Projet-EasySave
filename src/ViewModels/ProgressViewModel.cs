using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace EasySave.ViewModels;

/// <summary>
/// Tracks progress for a single backup job during parallel execution
/// </summary>
public class JobProgressItem : ViewModelBase
{
    private string _jobId = string.Empty;
    private string _jobName = string.Empty;
    private string _currentFile = string.Empty;
    private int _totalFiles;
    private int _filesProcessed;
    private int _progressPercentage;
    private long _totalSize;
    private long _processedSize;
    private bool _isCompleted;
    private bool _hasError;
    private bool _isPaused;
    private string _errorMessage = string.Empty;
    private DateTime _startTime;
    private int _encryptedFilesCount;

    public string JobId
    {
        get => _jobId;
        set => SetProperty(ref _jobId, value);
    }

    public string JobName
    {
        get => _jobName;
        set => SetProperty(ref _jobName, value);
    }

    public string CurrentFile
    {
        get => _currentFile;
        set => SetProperty(ref _currentFile, value);
    }

    public int TotalFiles
    {
        get => _totalFiles;
        set
        {
            if (SetProperty(ref _totalFiles, value))
                OnPropertyChanged(nameof(FilesDisplay));
        }
    }

    public int FilesProcessed
    {
        get => _filesProcessed;
        set
        {
            if (SetProperty(ref _filesProcessed, value))
                OnPropertyChanged(nameof(FilesDisplay));
        }
    }

    public int ProgressPercentage
    {
        get => _progressPercentage;
        set => SetProperty(ref _progressPercentage, value);
    }

    public long TotalSize
    {
        get => _totalSize;
        set
        {
            if (SetProperty(ref _totalSize, value))
                OnPropertyChanged(nameof(SizeDisplay));
        }
    }

    public long ProcessedSize
    {
        get => _processedSize;
        set
        {
            if (SetProperty(ref _processedSize, value))
            {
                OnPropertyChanged(nameof(SizeDisplay));
                OnPropertyChanged(nameof(SpeedDisplay));
                OnPropertyChanged(nameof(ElapsedDisplay));
                OnPropertyChanged(nameof(EstimatedDisplay));
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanResume));
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            if (SetProperty(ref _hasError, value))
            {
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanResume));
            }
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (SetProperty(ref _isPaused, value))
            {
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanResume));
            }
        }
    }

    public bool CanPause => !IsCompleted && !HasError && !IsPaused;
    public bool CanResume => !IsCompleted && !HasError && IsPaused;

    public int EncryptedFilesCount
    {
        get => _encryptedFilesCount;
        set
        {
            if (SetProperty(ref _encryptedFilesCount, value))
            {
                OnPropertyChanged(nameof(EncryptedFilesDisplay));
                OnPropertyChanged(nameof(HasEncryptedFiles));
            }
        }
    }

    public string EncryptedFilesDisplay => _encryptedFilesCount > 0
        ? $"{_encryptedFilesCount} fichier(s) chiffré(s)"
        : string.Empty;

    public bool HasEncryptedFiles => _encryptedFilesCount > 0;

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string FilesDisplay => $"{FilesProcessed} / {TotalFiles}";

    public string SizeDisplay => $"{FormatBytes(ProcessedSize)} / {FormatBytes(TotalSize)}";

    public string SpeedDisplay
    {
        get
        {
            if (_startTime == default || ProcessedSize == 0) return "-- /s";
            var elapsed = (DateTime.Now - _startTime).TotalSeconds;
            if (elapsed < 0.5) return "-- /s";
            var bytesPerSec = ProcessedSize / elapsed;
            return $"{FormatBytes((long)bytesPerSec)}/s";
        }
    }

    public string ElapsedDisplay
    {
        get
        {
            if (_startTime == default) return "--:--";
            var elapsed = DateTime.Now - _startTime;
            return elapsed.TotalHours >= 1
                ? elapsed.ToString(@"hh\:mm\:ss")
                : elapsed.ToString(@"mm\:ss");
        }
    }

    public string EstimatedDisplay
    {
        get
        {
            if (_startTime == default || ProcessedSize == 0 || TotalSize == 0) return "...";
            var elapsed = (DateTime.Now - _startTime).TotalSeconds;
            if (elapsed < 0.5) return "...";
            var bytesPerSec = ProcessedSize / elapsed;
            if (bytesPerSec < 1) return "...";
            var remainingBytes = TotalSize - ProcessedSize;
            var seconds = remainingBytes / bytesPerSec;
            if (seconds < 60) return $"~{(int)seconds}s";
            if (seconds < 3600) return $"~{(int)(seconds / 60)}min";
            return $"~{seconds / 3600:F1}h";
        }
    }

    public void StartTracking()
    {
        _startTime = DateTime.Now;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}

/// <summary>
/// ViewModel for displaying backup progress in real-time, supporting multiple concurrent jobs.
/// </summary>
public class ProgressViewModel : ViewModelBase
{
    private bool _isCompleted;

    /// <summary>Collection of individual job progress items (one per running job).</summary>
    public ObservableCollection<JobProgressItem> Jobs { get; } = new();

    /// <summary>Overall progress percentage weighted by size across all jobs.</summary>
    public int OverallProgressPercentage
    {
        get
        {
            var totalSize = Jobs.Sum(j => j.TotalSize);
            if (totalSize == 0) return 0;
            var processedSize = Jobs.Sum(j => j.ProcessedSize);
            return (int)(processedSize * 100 / totalSize);
        }
    }

    /// <summary>True when all jobs have completed or errored (and at least one exists).</summary>
    public bool AllCompleted => Jobs.Count > 0 && Jobs.All(j => j.IsCompleted || j.HasError);

    public int CompletedJobsCount => Jobs.Count(j => j.IsCompleted);
    public int ErrorJobsCount => Jobs.Count(j => j.HasError);
    public int TotalJobsCount => Jobs.Count;

    /// <summary>True when there are multiple jobs tracked simultaneously.</summary>
    public bool IsMultiJob => Jobs.Count > 1;

    /// <summary>True when all jobs have finished and their entries have been removed.</summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    /// <summary>
    /// Adds a job to track in multi-job mode
    /// </summary>
    public JobProgressItem AddJob(string jobId, string jobName)
    {
        var item = new JobProgressItem { JobId = jobId, JobName = jobName };
        item.StartTracking();
        Jobs.Add(item);
        OnPropertyChanged(nameof(IsMultiJob));
        OnPropertyChanged(nameof(TotalJobsCount));
        return item;
    }

    /// <summary>
    /// Marks a job as completed and updates aggregate properties
    /// </summary>
    public void MarkJobCompleted(string jobId, int encryptedFilesCount = 0)
    {
        var job = Jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job != null)
        {
            job.IsCompleted = true;
            job.ProgressPercentage = 100;
            job.EncryptedFilesCount = encryptedFilesCount;
            OnPropertyChanged(nameof(CompletedJobsCount));
            OnPropertyChanged(nameof(AllCompleted));
            OnPropertyChanged(nameof(OverallProgressPercentage));

            ScheduleRemoveJob(job);
        }
    }

    /// <summary>
    /// Marks a job as failed
    /// </summary>
    public void MarkJobError(string jobId, string errorMessage)
    {
        var job = Jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job != null)
        {
            job.HasError = true;
            job.ErrorMessage = errorMessage;
            OnPropertyChanged(nameof(ErrorJobsCount));
            OnPropertyChanged(nameof(AllCompleted));

            ScheduleRemoveJob(job, 5);
        }
    }

    /// <summary>
    /// Removes a finished job from the list after a delay, then checks if all done.
    /// </summary>
    private async void ScheduleRemoveJob(JobProgressItem job, int delaySeconds = 3)
    {
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        Dispatcher.UIThread.Post(() =>
        {
            Jobs.Remove(job);
            OnPropertyChanged(nameof(TotalJobsCount));
            OnPropertyChanged(nameof(CompletedJobsCount));
            OnPropertyChanged(nameof(ErrorJobsCount));
            OnPropertyChanged(nameof(IsMultiJob));
            OnPropertyChanged(nameof(AllCompleted));
            OnPropertyChanged(nameof(OverallProgressPercentage));

            if (Jobs.Count == 0)
                IsCompleted = true;
        });
    }

    /// <summary>
    /// Removes a job that was stopped (blocked-app detection) without marking it as error.
    /// </summary>
    public void MarkJobStopped(string jobId)
    {
        var job = Jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job != null)
        {
            job.IsCompleted = true;
            OnPropertyChanged(nameof(AllCompleted));
            ScheduleRemoveJob(job, 1);
        }
    }

    /// <summary>
    /// Marks a job as paused in the UI.
    /// </summary>
    public void MarkJobPaused(string jobId)
    {
        var job = Jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job is not null) job.IsPaused = true;
    }

    /// <summary>
    /// Marks a job as resumed (no longer paused) in the UI.
    /// </summary>
    public void MarkJobResumed(string jobId)
    {
        var job = Jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job is not null) job.IsPaused = false;
    }

    /// <summary>
    /// Updates progress for a specific job
    /// </summary>
    public void UpdateJobProgress(string jobId, string currentFile, int totalFiles, int filesProcessed,
        int progressPercentage, long totalSize, long processedSize)
    {
        var job = Jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job != null)
        {
            job.CurrentFile = currentFile;
            job.TotalFiles = totalFiles;
            job.FilesProcessed = filesProcessed;
            job.ProgressPercentage = progressPercentage;
            job.TotalSize = totalSize;
            job.ProcessedSize = processedSize;
            OnPropertyChanged(nameof(OverallProgressPercentage));
        }
    }

    /// <summary>
    /// Resets all progress values to initial state
    /// </summary>
    public void Reset()
    {
        IsCompleted = false;
        Jobs.Clear();
        OnPropertyChanged(nameof(IsMultiJob));
        OnPropertyChanged(nameof(AllCompleted));
        OnPropertyChanged(nameof(OverallProgressPercentage));
        OnPropertyChanged(nameof(TotalJobsCount));
        OnPropertyChanged(nameof(CompletedJobsCount));
        OnPropertyChanged(nameof(ErrorJobsCount));
    }
}
