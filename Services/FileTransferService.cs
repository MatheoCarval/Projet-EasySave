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

namespace EasySave.Services
{
    public class FileTransferService
    {
        
        private readonly ILogger _logger;  
        private readonly StateWriter _stateWriter;
        
        
        public FileTransferService(
            ILogger logger,  
            StateWriter stateWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));
        }
        
        // ==================== PUBLIC METHODS ====================
        
        /// <summary>
        /// Transfer an entire directory (recursive) from source to target
        /// </summary>
        public void TransferDirectory(string sourceDir, string targetDir, BackupJob job)
        {
            if (!PathValidator.PathExists(sourceDir))
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
            
            if (!PathValidator.IsDirectory(sourceDir))
                throw new InvalidOperationException($"Source path is not a directory: {sourceDir}");
            
            var allFiles = GetAllFiles(sourceDir);
            
            job.TotalFiles = allFiles.Count;
            job.TotalSize = allFiles.Sum(f => FileSystemHelper.GetFileSize(f));
            job.RemainingFiles = job.TotalFiles;
            job.RemainingSize = job.TotalSize;
            _stateWriter.UpdateJobState(job);
            
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
            
            job.MarkAsCompleted();
            _stateWriter.UpdateJobState(job);
        }
        
        /// <summary>
        /// Transfer a single file from source to target
        /// </summary>
        public void TransferFile(string sourceFile, string targetFile, BackupJob job)
        {
            job.SetCurrentFile(
                PathValidator.ToUncPath(sourceFile), 
                PathValidator.ToUncPath(targetFile)
            );
            _stateWriter.UpdateJobState(job);
            
            CreateDirectoryStructure(Path.GetDirectoryName(targetFile));
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            long fileSize = FileSystemHelper.GetFileSize(sourceFile);
            long transferTime = -1; // Default: error
            
            try
            {
                transferTime = CopyFile(sourceFile, targetFile);
                stopwatch.Stop();
                
                // 4. Log successful transfer
                var logEntry = new BackupLogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePath = PathValidator.ToUncPath(sourceFile),
                    TargetPath = PathValidator.ToUncPath(targetFile),
                    FileSize = fileSize,
                    TransferTime = stopwatch.ElapsedMilliseconds
                };
                
                _logger.Log(logEntry);
                
                job.RemainingFiles--;
                job.RemainingSize -= fileSize;
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
                    TransferTime = -1 
                };
                
                _logger.Log(logEntry);
                
                job.MarkAsError();
                _stateWriter.UpdateJobState(job);
                
                throw new FileTransferException($"Failed to transfer file: {sourceFile}", ex);
            }
        }
        
        // ==================== PRIVATE METHODS ====================
        
        /// <summary>
        /// Copy a file from source to destination and return elapsed time
        /// </summary>
        private long CopyFile(string source, string destination)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            // Use File.Copy with overwrite
            File.Copy(source, destination, overwrite: true);
            
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
        
        /// <summary>
        /// Get all files recursively from a directory
        /// </summary>
        private List<string> GetAllFiles(string directory)
        {
            var files = new List<string>();
            
            try
            {
                // Get files in current directory
                files.AddRange(Directory.GetFiles(directory));
                
                // Get files in subdirectories (recursive)
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
        /// Create directory structure for target path
        /// </summary>
        private void CreateDirectoryStructure(string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
                
            }
        }
        
        /// <summary>
        /// Determine if a file should be copied based on backup type
        /// </summary>
        private bool ShouldCopyFile(string sourceFile, string targetFile, BackupType type)
        {
            switch (type)
            {
                case BackupType.COMPLETE:
                    // Always copy in complete backup
                    return true;
                
                case BackupType.DIFFERENTIAL:
                    // Copy only if:
                    // 1. Target doesn't exist
                    // 2. Source is newer than target
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
    
    
    public class FileTransferException : Exception
    {
        public FileTransferException(string message) : base(message) { }
        public FileTransferException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}