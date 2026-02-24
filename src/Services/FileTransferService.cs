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
        /// Ordered list of priority extensions (first = highest priority). Files with these extensions
        /// are copied before others within a single backup job.
        /// </summary>
        private List<string> _priorityExtensions = new();

        /// <summary>
        /// Shared throttle that limits total bytes in-flight across all parallel backup jobs.
        /// Null or HasLimit=false means no restriction.
        /// </summary>
        private TransferThrottle? _throttle;

        /// <summary>
        /// Updates the priority extensions list used to sort files before transfer.
        /// </summary>
        public void SetPriorityExtensions(IEnumerable<string>? extensions)
        {
            _priorityExtensions = extensions?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Sets (or clears) the shared byte-level throttle for parallel transfers.
        /// </summary>
        public void SetThrottle(TransferThrottle? throttle)
        {
            _throttle = throttle;
        }

        /// <summary>
        /// Event raised when a file transfer completes (throttled)
        /// </summary>
        public event EventHandler<FileProgressEventArgs>? FileTransferred;
        private DateTime _lastProgressEvent = DateTime.MinValue;
        private const int ProgressEventIntervalMs = 150;

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

        public void FlushLogger()
        {
            _logger.Flush();
        }

        /// <summary>
        /// Recursively transfers all files from the source directory to the target directory, skipping files based on the backup type and updating job progress.
        /// </summary>
        public void TransferDirectory(string sourceDir, string targetDir, BackupJob job, PauseToken? pauseToken = null)
        {
            if (!PathValidator.PathExists(sourceDir))
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            if (!PathValidator.IsDirectory(sourceDir))
                throw new InvalidOperationException($"Source path is not a directory: {sourceDir}");

            var allFiles = GetAllFiles(sourceDir);

            // Sort: priority extensions first (in priority order), then the rest alphabetically
            if (_priorityExtensions.Count > 0)
            {
                allFiles = allFiles
                    .OrderBy(f =>
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        var idx = _priorityExtensions.IndexOf(ext);
                        return idx >= 0 ? idx : _priorityExtensions.Count;
                    })
                    .ThenBy(f => f)
                    .ToList();
            }

            if (_throttle == null || !_throttle.HasLimit)
            {
                // Simple sequential loop — no throttle
                foreach (var sourceFile in allFiles)
                {
                    pauseToken?.WaitIfPaused();

                    string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                    string targetFile = Path.Combine(targetDir, relativePath);

                    if (ShouldCopyFile(sourceFile, targetFile, job.BackupType))
                        TransferFileCore(sourceFile, targetFile, job, pauseToken);
                    else
                    {
                        job.RemainingFiles--;
                        job.RemainingSize -= FileSystemHelper.GetFileSize(sourceFile);
                        job.UpdateProgress();
                    }
                }
            }
            else
            {
                // Throttled loop: skip ahead to find the first file that fits within remaining capacity.
                // Pre-compute sizes once to avoid repeated filesystem calls in AcquireFirstFitting.
                var pending = allFiles
                    .Select(f => (Path: f, Size: FileSystemHelper.GetFileSize(f)))
                    .ToList();

                while (pending.Count > 0)
                {
                    // Check pause BEFORE acquiring a throttle slot (avoids holding a slot while paused)
                    pauseToken?.WaitIfPaused();

                    var sizes = pending.Select(p => p.Size).ToList();
                    int idx = _throttle.AcquireFirstFitting(sizes); // blocks until a slot is free
                    var chosen = pending[idx];
                    pending.RemoveAt(idx);

                    string relativePath = Path.GetRelativePath(sourceDir, chosen.Path);
                    string targetFile = Path.Combine(targetDir, relativePath);

                    if (ShouldCopyFile(chosen.Path, targetFile, job.BackupType))
                    {
                        try
                        {
                            TransferFileCore(chosen.Path, targetFile, job, pauseToken);
                        }
                        finally
                        {
                            _throttle.Release(chosen.Size);
                        }
                    }
                    else
                    {
                        _throttle.Release(chosen.Size);
                        job.RemainingFiles--;
                        job.RemainingSize -= chosen.Size;
                        job.UpdateProgress();
                    }
                }
            }
        }

        /// <summary>
        /// Transfers a single file from source to target (public entry point for single-file jobs).
        /// Acquires the shared throttle slot before copying and releases it after.
        /// </summary>
        public void TransferFile(string sourceFile, string targetFile, BackupJob job, PauseToken? pauseToken = null)
        {
            pauseToken?.WaitIfPaused();

            long fileSize = FileSystemHelper.GetFileSize(sourceFile);
            _throttle?.Acquire(fileSize);
            try
            {
                TransferFileCore(sourceFile, targetFile, job, pauseToken);
            }
            finally
            {
                _throttle?.Release(fileSize);
            }
        }

        /// <summary>
        /// Core file transfer logic: copies the file, logs metrics, updates job progress.
        /// Called by TransferFile (throttle already acquired) and by TransferDirectory (throttle managed externally).
        /// </summary>
        private void TransferFileCore(string sourceFile, string targetFile, BackupJob job, PauseToken? pauseToken = null)
        {
            // Block here if the job is paused (important for direct single-file calls)
            pauseToken?.WaitIfPaused();

            job.SetCurrentFile(
                PathValidator.ToUncPath(sourceFile),
                PathValidator.ToUncPath(targetFile)
            );

            string? targetDirectory = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetDirectory))
            {
                CreateDirectoryStructure(targetDirectory);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            long fileSize = FileSystemHelper.GetFileSize(sourceFile);

            try
            {
                // Track bytes written during chunked copy for real-time progress on large files
                long bytesInCurrentFile = 0;
                int chunkIndex = 0;
                CopyFile(sourceFile, targetFile, chunkBytes =>
                {
                    bytesInCurrentFile += chunkBytes;

                    // Skip the DateTime check for most chunks — only check every 8th chunk (every ~2 MB)
                    // to avoid calling DateTime.UtcNow thousands of times per second on fast storage.
                    if ((chunkIndex++ & 7) != 0) return;

                    // Throttled intermediate progress event — fires every ProgressEventIntervalMs even
                    // within a single large file so the UI progresses smoothly
                    var chunkNow = DateTime.UtcNow;
                    if ((chunkNow - _lastProgressEvent).TotalMilliseconds >= ProgressEventIntervalMs)
                    {
                        _lastProgressEvent = chunkNow;
                        var effectiveRemaining = Math.Max(0, job.RemainingSize - bytesInCurrentFile);
                        var effectiveProgress = job.TotalSize > 0
                            ? (int)(100.0 * (job.TotalSize - effectiveRemaining) / job.TotalSize)
                            : 0;
                        FileTransferred?.Invoke(this, new FileProgressEventArgs
                        {
                            JobId = job.Id,
                            JobName = job.Name,
                            CurrentFile = Path.GetFileName(sourceFile),
                            TotalFiles = (int)job.TotalFiles,
                            RemainingFiles = (int)job.RemainingFiles,
                            TotalSize = job.TotalSize,
                            RemainingSize = effectiveRemaining,
                            ProgressPercentage = effectiveProgress
                        });
                    }
                });

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

                // Final event after file completes (always fires to ensure exact end state)
                var now = DateTime.UtcNow;
                if ((now - _lastProgressEvent).TotalMilliseconds >= ProgressEventIntervalMs
                    || job.RemainingFiles <= 0)
                {
                    _lastProgressEvent = now;
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
        private const int SmallFileThreshold = 1024 * 1024; // 1 MB
        private const int LargeBufferSize = 256 * 1024;    // 256 KB buffer

        /// <summary>
        /// Copies a file from source to destination.
        /// For large files, copies in chunks and invokes <paramref name="onChunkWritten"/> after each chunk
        /// so callers can report real-time progress.
        /// </summary>
        private long CopyFile(string source, string destination, Action<long>? onChunkWritten = null)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // If destination exists with ReadOnly attribute, overwrite fails — clear it first
            if (File.Exists(destination))
            {
                var attrs = File.GetAttributes(destination);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(destination, attrs & ~FileAttributes.ReadOnly);
            }

            var sourceInfo = new FileInfo(source);
            if (sourceInfo.Length > SmallFileThreshold)
            {
                // Large file: manual chunked copy so we can report progress per chunk
                using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, LargeBufferSize, FileOptions.SequentialScan);
                using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, LargeBufferSize, FileOptions.SequentialScan);

                byte[] buffer = new byte[LargeBufferSize];
                int bytesRead;
                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    destStream.Write(buffer, 0, bytesRead);
                    onChunkWritten?.Invoke(bytesRead);
                }
            }
            else
            {
                File.Copy(source, destination, overwrite: true);
            }

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