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
    private string _errorMessage = string.Empty;
    private DateTime _startTime;

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
        set => SetProperty(ref _isCompleted, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

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
/// ViewModel for displaying backup progress in real-time, supporting multiple concurrent jobs
/// </summary>
public class ProgressViewModel : ViewModelBase
{
    private string _jobName = string.Empty;
    private string _currentFile = string.Empty;
    private int _totalFiles;
    private int _filesProcessed;
    private int _progressPercentage;
    private long _totalSize;
    private long _processedSize;
    private bool _isCompleted;
    private DateTime _startTime;

    /// <summary>
    /// Collection of individual job progress items for parallel execution
    /// </summary>
    public ObservableCollection<JobProgressItem> Jobs { get; } = new();

    /// <summary>
    /// Overall progress percentage across all jobs
    /// </summary>
    public int OverallProgressPercentage
    {
        get
        {
            if (Jobs.Count == 0) return _progressPercentage;
            var totalSize = Jobs.Sum(j => j.TotalSize);
            if (totalSize == 0) return 0;
            var processedSize = Jobs.Sum(j => j.ProcessedSize);
            return (int)(processedSize * 100 / totalSize);
        }
    }

    /// <summary>
    /// True when all jobs in the collection have completed or errored
    /// </summary>
    public bool AllCompleted => Jobs.Count > 0 && Jobs.All(j => j.IsCompleted || j.HasError);

    public int CompletedJobsCount => Jobs.Count(j => j.IsCompleted);
    public int ErrorJobsCount => Jobs.Count(j => j.HasError);
    public int TotalJobsCount => Jobs.Count;

    /// <summary>
    /// Whether we're in multi-job mode
    /// </summary>
    public bool IsMultiJob => Jobs.Count > 1;

    // ── Single-job properties (backward compatibility) ──

    /// <summary>
    /// Name of the backup job being executed
    /// </summary>
    public string JobName
    {
        get => _jobName;
        set => SetProperty(ref _jobName, value);
    }

    /// <summary>
    /// Name of the file currently being transferred
    /// </summary>
    public string CurrentFile
    {
        get => _currentFile;
        set => SetProperty(ref _currentFile, value);
    }

    /// <summary>
    /// Total number of files to transfer
    /// </summary>
    public int TotalFiles
    {
        get => _totalFiles;
        set
        {
            if (SetProperty(ref _totalFiles, value))
            {
                OnPropertyChanged(nameof(FilesDisplay));
            }
        }
    }

    /// <summary>
    /// Number of files processed so far
    /// </summary>
    public int FilesProcessed
    {
        get => _filesProcessed;
        set
        {
            if (SetProperty(ref _filesProcessed, value))
            {
                OnPropertyChanged(nameof(FilesDisplay));
            }
        }
    }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage
    {
        get => _progressPercentage;
        set => SetProperty(ref _progressPercentage, value);
    }

    /// <summary>
    /// Total size in bytes
    /// </summary>
    public long TotalSize
    {
        get => _totalSize;
        set
        {
            if (SetProperty(ref _totalSize, value))
            {
                OnPropertyChanged(nameof(SizeDisplay));
            }
        }
    }

    /// <summary>
    /// Processed size in bytes
    /// </summary>
    public long ProcessedSize
    {
        get => _processedSize;
        set
        {
            if (SetProperty(ref _processedSize, value))
            {
                OnPropertyChanged(nameof(SizeDisplay));
                OnPropertyChanged(nameof(ElapsedTimeDisplay));
                OnPropertyChanged(nameof(TransferSpeedDisplay));
                OnPropertyChanged(nameof(EstimatedTimeDisplay));
            }
        }
    }

    /// <summary>
    /// Display string for file count: "5 / 100 files"
    /// </summary>
    public string FilesDisplay => $"{FilesProcessed} / {TotalFiles} files";

    /// <summary>
    /// Display string for size: "2.5 MB / 10 MB"
    /// </summary>
    public string SizeDisplay => $"{FormatBytes(ProcessedSize)} / {FormatBytes(TotalSize)}";

    /// <summary>
    /// Elapsed time since backup started
    /// </summary>
    public string ElapsedTimeDisplay
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

    /// <summary>
    /// Current transfer speed
    /// </summary>
    public string TransferSpeedDisplay
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

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public string EstimatedTimeDisplay
    {
        get
        {
            if (_startTime == default || ProcessedSize == 0 || TotalSize == 0) return "Calculating...";
            var elapsed = (DateTime.Now - _startTime).TotalSeconds;
            if (elapsed < 0.5) return "Calculating...";
            var bytesPerSec = ProcessedSize / elapsed;
            if (bytesPerSec < 1) return "Calculating...";
            var remainingBytes = TotalSize - ProcessedSize;
            var seconds = remainingBytes / bytesPerSec;
            if (seconds < 60) return $"~{(int)seconds}s remaining";
            if (seconds < 3600) return $"~{(int)(seconds / 60)}min remaining";
            return $"~{seconds / 3600:F1}h remaining";
        }
    }

    /// <summary>
    /// Starts tracking elapsed time
    /// </summary>
    public void StartTracking()
    {
        _startTime = DateTime.Now;
    }

    /// <summary>
    /// Indicates whether the backup has completed
    /// </summary>
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
    public void MarkJobCompleted(string jobId)
    {
        var job = Jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job != null)
        {
            job.IsCompleted = true;
            job.ProgressPercentage = 100;
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
    /// Formats bytes to a human-readable format (KB, MB, GB)
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    /// <summary>
    /// Resets all progress values to initial state
    /// </summary>
    public void Reset()
    {
        JobName = string.Empty;
        CurrentFile = string.Empty;
        TotalFiles = 0;
        FilesProcessed = 0;
        ProgressPercentage = 0;
        TotalSize = 0;
        ProcessedSize = 0;
        IsCompleted = false;
        _startTime = default;
        Jobs.Clear();
        OnPropertyChanged(nameof(IsMultiJob));
        OnPropertyChanged(nameof(AllCompleted));
        OnPropertyChanged(nameof(OverallProgressPercentage));
        OnPropertyChanged(nameof(TotalJobsCount));
        OnPropertyChanged(nameof(CompletedJobsCount));
        OnPropertyChanged(nameof(ErrorJobsCount));
    }
}
