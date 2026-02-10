using System;

namespace EasySave.ViewModels;

/// <summary>
/// ViewModel for displaying backup progress in real-time
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
    /// Indicates whether the backup has completed
    /// </summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    /// <summary>
    /// Formats bytes to a human-readable format (KB, MB, GB)
    /// </summary>
    private string FormatBytes(long bytes)
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
    }
}
