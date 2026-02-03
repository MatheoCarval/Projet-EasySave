using System;
using System.Collections.Generic;
using Models.Enums;

namespace Models
{
    public class BackupJob
    {
        public string Name { get; set; }
        public List<string> SourcePath { get; set; }
        public string TargetPath { get; set; }
        public BackupType BackupType { get; set; }
        public BackupState BackupState { get; set; }
        public DateTime LastExecution { get; set; }
        public long TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public long RemainingFiles { get; set; }
        public long RemainingSize { get; set; }
        public string? CurrentSourceFile { get; set; }
        public string? CurrentTargetFile { get; set; }
        public float Progress { get; set; }

        public BackupJob(string name, string sourcePath, string targetPath, BackupType backupType)
        {
            Name = name;
            SourcePath = new List<string> { sourcePath };
            TargetPath = targetPath;
            BackupType = backupType;
            BackupState = BackupState.PAUSED;
            LastExecution = DateTime.MinValue;
            TotalFiles = 0;
            TotalSize = 0;
            RemainingFiles = 0;
            RemainingSize = 0;
            CurrentSourceFile = null;
            CurrentTargetFile = null;
            Progress = 0;
        }

        public void UpdateProgress()
        {
            if (TotalFiles > 0)
            {
                Progress = ((TotalSize - RemainingSize) * 100 / TotalSize);
            }
        }

        public void SetCurrentFile(string sourceFile, string targetFile)
        {
            CurrentSourceFile = sourceFile;
            CurrentTargetFile = targetFile;
        }

        public void MarkAsCompleted()
        {
            LastExecution = DateTime.Now;
            RemainingFiles = 0;
            RemainingSize = 0;
            Progress = 100;
            BackupState = BackupState.COMPLETED;
        }

        public void MarkAsError()
        {
            Progress = 0;
            CurrentSourceFile = null;
            CurrentTargetFile = null;
            BackupState = BackupState.ERROR;
        }
    }
}
