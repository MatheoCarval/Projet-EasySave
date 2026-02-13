using System;
using System.Collections.Generic;
using Models.Enums;

namespace Models
{
    /// <summary>
    /// Represents a backup job with configuration, state tracking, and progress monitoring capabilities.
    /// </summary>
    public class BackupJob
    {
        /// <summary>
        /// Unique identifier for the backup job, generated as a GUID.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Human-readable name of the backup job.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Collection of source directory paths to be backed up.
        /// </summary>
        public List<string> SourcePath { get; set; }
        /// <summary>
        /// Destination directory path where backup files are stored.
        /// </summary>
        public string TargetPath { get; set; }
        /// <summary>
        /// The type of backup to perform (Complete, Differential, Incremental).
        /// </summary>
        public BackupType BackupType { get; set; }
        /// <summary>
        /// Current state of the backup job (Active, Paused, Completed, Error, Pending).
        /// </summary>
        public BackupState BackupState { get; set; }
        /// <summary>
        /// Timestamp of the most recent backup job execution.
        /// </summary>
        public DateTime LastExecution { get; set; }
        /// <summary>
        /// Total number of files in the backup source.
        /// </summary>
        public long TotalFiles { get; set; }
        /// <summary>
        /// Total size in bytes of all files in the backup source.
        /// </summary>
        public long TotalSize { get; set; }
        /// <summary>
        /// Number of files remaining to be backed up.
        /// </summary>
        public long RemainingFiles { get; set; }
        /// <summary>
        /// Total size in bytes of files remaining to be backed up.
        /// </summary>
        public long RemainingSize { get; set; }
        /// <summary>
        /// Full path of the current source file being processed.
        /// </summary>
        public string? CurrentSourceFile { get; set; }
        /// <summary>
        /// Full path of the current destination file being processed.
        /// </summary>
        public string? CurrentTargetFile { get; set; }
        /// <summary>
        /// Progress percentage of the backup operation from 0 to 100.
        /// </summary>
        public float Progress { get; set; }
        /// <summary>
        /// Indicates whether files should be encrypted after being saved.
        /// </summary>
        public bool EncryptFiles { get; set; }
        /// <summary>
        /// List of file extensions specific to this backup job that should be encrypted (in addition to global settings).
        /// </summary>
        public List<string> EncryptedExtensions { get; set; }

        /// <summary>
        /// Initializes a new instance of BackupJob with default values; required for JSON deserialization.
        /// </summary>
        public BackupJob()
        {
            Name = string.Empty;
            SourcePath = new List<string>();
            TargetPath = string.Empty;
            BackupType = BackupType.COMPLETE;
            BackupState = BackupState.PENDING;
            LastExecution = DateTime.MinValue;
            TotalFiles = 0;
            TotalSize = 0;
            RemainingFiles = 0;
            RemainingSize = 0;
            CurrentSourceFile = null;
            CurrentTargetFile = null;
            Progress = 0;
            EncryptFiles = false;
            EncryptedExtensions = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of BackupJob with specified name, source paths, target path, and backup type.
        /// </summary>
        public BackupJob(string name, List<string> sourcePath, string targetPath, BackupType backupType)
        {
            Name = name;
            SourcePath = sourcePath;
            TargetPath = targetPath;
            BackupType = backupType;
            BackupState = BackupState.PENDING;
            LastExecution = DateTime.MinValue;
            TotalFiles = 0;
            TotalSize = 0;
            RemainingFiles = 0;
            RemainingSize = 0;
            CurrentSourceFile = null;
            CurrentTargetFile = null;
            Progress = 0;
            EncryptFiles = false;
            EncryptedExtensions = new List<string>();
        }

        /// <summary>
        /// Calculates and updates the progress percentage based on the ratio of transferred size to total size.
        /// </summary>
        public void UpdateProgress()
        {
            if (TotalSize > 0)
            {
                Progress = ((TotalSize - RemainingSize) * 100 / TotalSize);
            }
        }

        /// <summary>
        /// Sets the current source and destination file paths being processed during backup execution.
        /// </summary>
        public void SetCurrentFile(string sourceFile, string targetFile)
        {
            CurrentSourceFile = sourceFile;
            CurrentTargetFile = targetFile;
        }

        /// <summary>
        /// Marks the backup job as completed by updating the last execution time, clearing remaining files and size, and setting progress to 100%.
        /// </summary>
        public void MarkAsCompleted()
        {
            LastExecution = DateTime.Now;
            RemainingFiles = 0;
            RemainingSize = 0;
            Progress = 100;
            BackupState = BackupState.COMPLETED;
        }

        /// <summary>
        /// Marks the backup job as failed by resetting progress to 0 and clearing current file paths.
        /// </summary>
        public void MarkAsError()
        {
            Progress = 0;
            CurrentSourceFile = null;
            CurrentTargetFile = null;
            BackupState = BackupState.ERROR;
        }
    }
}