using Models;
using Models.Enums;
using Xunit;
using System;

namespace EasySave.Tests.Models
{
    public class BackupJobTests
    {
        [Fact]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            // Arrange & Act
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            // Assert
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

        [Fact]
        public void UpdateProgress_WithValidValues_CalculatesCorrectly()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.TotalSize = 1000;
            job.RemainingSize = 500;

            // Act
            job.UpdateProgress();

            // Assert
            Assert.Equal(50, job.Progress);
        }

        [Fact]
        public void UpdateProgress_WithZeroTotalSize_DoesNotCrash()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.TotalSize = 0;
            job.RemainingSize = 0;

            // Act
            job.UpdateProgress();

            // Assert
            Assert.Equal(0, job.Progress);
        }

        [Fact]
        public void SetCurrentFile_UpdatesCurrentFiles()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            // Act
            job.SetCurrentFile(@"C:\Source\file.txt", @"C:\Target\file.txt");

            // Assert
            Assert.Equal(@"C:\Source\file.txt", job.CurrentSourceFile);
            Assert.Equal(@"C:\Target\file.txt", job.CurrentTargetFile);
        }

        [Fact]
        public void MarkAsCompleted_SetsPropertiesCorrectly()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.RemainingFiles = 5;
            job.RemainingSize = 500;
            var beforeTime = DateTime.Now;

            // Act
            job.MarkAsCompleted();
            var afterTime = DateTime.Now;

            // Assert
            Assert.Equal(0, job.RemainingFiles);
            Assert.Equal(0, job.RemainingSize);
            Assert.Equal(100, job.Progress);
            Assert.Equal(BackupState.COMPLETED, job.BackupState);
            Assert.InRange(job.LastExecution, beforeTime, afterTime);
        }

        [Fact]
        public void MarkAsError_ResetsPropertiesCorrectly()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.SetCurrentFile("source.txt", "target.txt");
            job.Progress = 50;

            // Act
            job.MarkAsError();

            // Assert
            Assert.Equal(0, job.Progress);
            Assert.Null(job.CurrentSourceFile);
            Assert.Null(job.CurrentTargetFile);
            Assert.Equal(BackupState.ERROR, job.BackupState);
        }

        [Theory]
        [InlineData(BackupType.COMPLETE)]
        [InlineData(BackupType.DIFFERENTIAL)]
        public void Constructor_WithDifferentBackupTypes_WorksCorrectly(BackupType backupType)
        {
            // Arrange & Act
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", backupType);

            // Assert
            Assert.Equal(backupType, job.BackupType);
        }
    }
}