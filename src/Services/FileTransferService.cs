using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

using Models.Entries;
using Models.Enums;
using Models;
using EasyLog.Abstractions;
using Utilities;
using Services.Writers;
using Services.Managers;

namespace EasySave.Services
{
    /// <summary>
    /// Manages file transfer operations for backup jobs, including directory traversal, file copying, and progress tracking with state persistence.
    /// </summary>
    public class FileTransferService
    {
        /// <summary>
        /// Logger instance for recording file transfer operations and backup events.
        /// </summary>
        private ILogger _logger;
        /// <summary>
        /// State writer instance for persisting backup job state during file transfer operations.
        /// </summary>
        private readonly StateWriter _stateWriter;
        /// <summary>
        /// Manager responsible for encryption of files after transfer.
        /// </summary>
        private readonly CryptageManager _cryptageManager;

        /// <summary>
        /// Event raised when a file transfer completes
        /// </summary>
        public event EventHandler<FileProgressEventArgs>? FileTransferred;

        /// <summary>
        /// Initializes FileTransferService with required dependencies for logging and state persistence.
        /// </summary>
        public FileTransferService(
            ILogger logger,
            StateWriter stateWriter,
            CryptageManager? cryptageManager = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));
            _cryptageManager = cryptageManager ?? new CryptageManager(string.Empty, string.Empty, Array.Empty<string>());
        }

        /// <summary>
        /// Updates the logger instance used for recording file transfer operations.
        /// This allows changing the log format (JSON/XML) without restarting the application.
        /// </summary>
        public void UpdateLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Recursively transfers all files from the source directory to the target directory, skipping files based on the backup type and updating job progress.
        /// </summary>
        public void TransferDirectory(string sourceDir, string targetDir, BackupJob job)
        {
            if (!PathValidator.PathExists(sourceDir))
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            if (!PathValidator.IsDirectory(sourceDir))
                throw new InvalidOperationException($"Source path is not a directory: {sourceDir}");

            var allFiles = GetAllFiles(sourceDir);

            foreach (var sourceFile in allFiles)
            {
                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string targetFile = Path.Combine(targetDir, relativePath);

                if (ShouldCopyFile(sourceFile, targetFile, job.BackupType))
                {
                    TransferFile(sourceFile, targetFile, job);
                }
                else
                {
                    job.RemainingFiles--;
                    job.RemainingSize -= FileSystemHelper.GetFileSize(sourceFile);
                }

                job.UpdateProgress();
                _stateWriter.UpdateJobState(job);
            }
        }

        /// <summary>
        /// Transfers a single file from source to target, records transfer metrics in the log, and updates job progress; throws FileTransferException on failure.
        /// </summary>
        public void TransferFile(string sourceFile, string targetFile, BackupJob job)
        {
            job.SetCurrentFile(
                PathValidator.ToUncPath(sourceFile),
                PathValidator.ToUncPath(targetFile)
            );
            _stateWriter.UpdateJobState(job);

            string? targetDirectory = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetDirectory))
            {
                CreateDirectoryStructure(targetDirectory);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            long fileSize = FileSystemHelper.GetFileSize(sourceFile);

            try
            {
                CopyFile(sourceFile, targetFile);
                stopwatch.Stop();
                long encryptionTime = _cryptageManager.EncryptIfNeeded(targetFile, job);

                var logEntry = new BackupLogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePath = PathValidator.ToUncPath(sourceFile),
                    TargetPath = PathValidator.ToUncPath(targetFile),
                    FileSize = fileSize,
                    TransferTime = stopwatch.ElapsedMilliseconds,
                    EncryptionTime = encryptionTime
                };

                _logger.Log(logEntry);

                job.RemainingFiles--;
                job.RemainingSize -= fileSize;
                job.UpdateProgress();
                _stateWriter.UpdateJobState(job);

                // Raise file transferred event
                FileTransferred?.Invoke(this, new FileProgressEventArgs
                {
                    JobId = job.Id,
                    JobName = job.Name,
                    CurrentFile = Path.GetFileName(sourceFile),
                    TotalFiles = (int)job.TotalFiles,
                    RemainingFiles = (int)job.RemainingFiles,
                    TotalSize = job.TotalSize,
                    RemainingSize = job.RemainingSize,
                    ProgressPercentage = (int)job.Progress
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var logEntry = new BackupLogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePath = PathValidator.ToUncPath(sourceFile),
                    TargetPath = PathValidator.ToUncPath(targetFile),
                    FileSize = fileSize,
                    TransferTime = -1,
                    EncryptionTime = 0
                };

                _logger.Log(logEntry);

                job.MarkAsError();
                _stateWriter.UpdateJobState(job);

                throw new FileTransferException($"Failed to transfer file: {sourceFile}", ex);
            }
        }

        /// <summary>
        /// Copies a file from source to destination using File.Copy with overwrite and measures elapsed time.
        /// </summary>
        private long CopyFile(string source, string destination)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            File.Copy(source, destination, overwrite: true);

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Recursively retrieves all files from a directory and its subdirectories, throwing FileTransferException on access denied errors.
        /// </summary>
        private List<string> GetAllFiles(string directory)
        {
            var files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(directory));

                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    files.AddRange(GetAllFiles(subDir));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new FileTransferException($"Access denied to directory: {directory}", ex);
            }

            return files;
        }

        /// <summary>
        /// Creates the target directory structure if it does not already exist.
        /// </summary>
        private void CreateDirectoryStructure(string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
        }

        /// <summary>
        /// Determines whether a file should be copied based on the backup type: always copies for complete backups, and only if target does not exist or source is newer for differential backups.
        /// </summary>
        private bool ShouldCopyFile(string sourceFile, string targetFile, BackupType type)
        {
            switch (type)
            {
                case BackupType.COMPLETE:
                    return true;

                case BackupType.DIFFERENTIAL:
                    if (!File.Exists(targetFile))
                        return true;

                    var sourceLastWrite = File.GetLastWriteTime(sourceFile);
                    var targetLastWrite = File.GetLastWriteTime(targetFile);

                    return sourceLastWrite > targetLastWrite;

                default:
                    return true;
            }
        }
    }


    /// <summary>
    /// Exception thrown when a file transfer operation fails during backup execution.
    /// </summary>
    public class FileTransferException : Exception
    {
        /// <summary>
        /// Initializes FileTransferException with a descriptive error message.
        /// </summary>
        public FileTransferException(string message) : base(message) { }
        /// <summary>
        /// Initializes FileTransferException with a descriptive error message and the underlying exception that caused the failure.
        /// </summary>
        public FileTransferException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}