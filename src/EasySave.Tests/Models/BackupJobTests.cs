using Models;
using Models.Enums;
using Xunit;
using System;

namespace EasySave.Tests.Models
{
    /// <summary>
    /// Unit tests for the BackupJob class, verifying initialization, progress calculation, file tracking, and state management.
    /// </summary>
    public class BackupJobTests
    {
        /// <summary>
        /// Verifies that the BackupJob constructor initializes all properties with correct values.
        /// </summary>
        [Fact]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            Assert.Equal("TestJob", job.Name);
            Assert.Single(job.SourcePath);
            Assert.Equal(@"C:\Source", job.SourcePath[0]);
            Assert.Equal(@"C:\Target", job.TargetPath);
            Assert.Equal(BackupType.COMPLETE, job.BackupType);
            Assert.Equal(BackupState.PENDING, job.BackupState);
            Assert.Equal(DateTime.MinValue, job.LastExecution);
            Assert.Equal(0, job.TotalFiles);
            Assert.Equal(0, job.TotalSize);
            Assert.Equal(0, job.RemainingFiles);
            Assert.Equal(0, job.RemainingSize);
            Assert.Null(job.CurrentSourceFile);
            Assert.Null(job.CurrentTargetFile);
            Assert.Equal(0, job.Progress);
        }

        /// <summary>
        /// Verifies that UpdateProgress calculates progress percentage correctly based on transferred and total size.
        /// </summary>
        [Fact]
        public void UpdateProgress_WithValidValues_CalculatesCorrectly()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.TotalSize = 1000;
            job.RemainingSize = 500;

            job.UpdateProgress();

            Assert.Equal(50, job.Progress);
        }

        /// <summary>
        /// Verifies that UpdateProgress handles zero total size without crashing or throwing an exception.
        /// </summary>
        [Fact]
        public void UpdateProgress_WithZeroTotalSize_DoesNotCrash()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.TotalSize = 0;
            job.RemainingSize = 0;

            job.UpdateProgress();

            Assert.Equal(0, job.Progress);
        }

        /// <summary>
        /// Verifies that SetCurrentFile correctly updates both source and target file path properties.
        /// </summary>
        [Fact]
        public void SetCurrentFile_UpdatesCurrentFiles()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            job.SetCurrentFile(@"C:\Source\file.txt", @"C:\Target\file.txt");

            Assert.Equal(@"C:\Source\file.txt", job.CurrentSourceFile);
            Assert.Equal(@"C:\Target\file.txt", job.CurrentTargetFile);
        }

        /// <summary>
        /// Verifies that MarkAsCompleted sets all completion properties including state, progress, and remaining counts.
        /// </summary>
        [Fact]
        public void MarkAsCompleted_SetsPropertiesCorrectly()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.RemainingFiles = 5;
            job.RemainingSize = 500;
            var beforeTime = DateTime.Now;

            job.MarkAsCompleted();
            var afterTime = DateTime.Now;

            Assert.Equal(0, job.RemainingFiles);
            Assert.Equal(0, job.RemainingSize);
            Assert.Equal(100, job.Progress);
            Assert.Equal(BackupState.COMPLETED, job.BackupState);
            Assert.InRange(job.LastExecution, beforeTime, afterTime);
        }

        /// <summary>
        /// Verifies that MarkAsError resets progress and file paths, setting the state to ERROR.
        /// </summary>
        [Fact]
        public void MarkAsError_ResetsPropertiesCorrectly()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.SetCurrentFile("source.txt", "target.txt");
            job.Progress = 50;

            job.MarkAsError();

            Assert.Equal(0, job.Progress);
            Assert.Null(job.CurrentSourceFile);
            Assert.Null(job.CurrentTargetFile);
            Assert.Equal(BackupState.ERROR, job.BackupState);
        }

        /// <summary>
        /// Verifies that the BackupJob constructor works correctly with different backup type values.
        /// </summary>
        [Theory]
        [InlineData(BackupType.COMPLETE)]
        [InlineData(BackupType.DIFFERENTIAL)]
        public void Constructor_WithDifferentBackupTypes_WorksCorrectly(BackupType backupType)
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", backupType);

            Assert.Equal(backupType, job.BackupType);
        }
    }
}