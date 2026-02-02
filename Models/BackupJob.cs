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
<<<<<<< HEAD
        public BackupState BackupType { get; set; }
=======
        public BackupType BackupType { get; set; }
>>>>>>> develop
        public BackupState BackupState { get; set; }
        public DateTime LastExecution { get; set; }
        public long TotalFile { get; set; }
        public long TotalSize { get; set; }
        public long RemainingFiles { get; set; }
        public long RemainingSize { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }
        public float Progress { get; set; }

        public BackupJob()
        {
            SourcePath = new List<string>();
        }

        public void UpdateProgress()
        {
            if (TotalFile > 0)
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
