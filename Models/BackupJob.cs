using System;
using Models.Enums;

namespace Models
{
    public class BackupJob
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public BackupState BackupType { get; set; }
        public BackupState BackupState { get; set; }
        public DateTime LastExecution { get; set; }
        public long TotalFile { get; set; }
        public long TotalSize { get; set; }
        public long RemainingFiles { get; set; }
        public long RemainingSize { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }
        public float Progress { get; set; }

        public void UpdateProgress()
        {
            if (TotalFile > 0)
            {
                Progress = ((TotalFile - RemainingFiles) * 100 / TotalFile);
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
