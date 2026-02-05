using Models;
using Models.Enums;
using Models.Entries;
using Xunit;

namespace EasySave.Tests.Models.Entries
{
    public class StateEntryTests
    {
        [Fact]
        public void FromBackupJob_CreatesStateEntryCorrectly()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.TotalFiles = 100;
            job.TotalSize = 5000;
            job.RemainingFiles = 50;
            job.RemainingSize = 2500;
            job.Progress = 50;
            job.SetCurrentFile(@"C:\Source\file.txt", @"C:\Target\file.txt");

            // Act
            var stateEntry = StateEntry.FromBackupJob(job);

            // Assert
            Assert.Equal("TestJob", stateEntry.JobName);
            Assert.Equal(BackupState.PENDING, stateEntry.State);
            Assert.Equal(100, stateEntry.TotalFiles);
            Assert.Equal(5000, stateEntry.TotalSize);
            Assert.Equal(50, stateEntry.RemainingFiles);
            Assert.Equal(2500, stateEntry.RemainingSize);
            Assert.Equal(50, stateEntry.Progress);
            Assert.Equal(@"C:\Source\file.txt", stateEntry.CurrentSourceFile);
            Assert.Equal(@"C:\Target\file.txt", stateEntry.CurrentTargetFile);
        }

        [Fact]
        public void FromBackupJob_WithCompletedJob_SetsCompletedState()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.MarkAsCompleted();

            // Act
            var stateEntry = StateEntry.FromBackupJob(job);

            // Assert
            Assert.Equal(BackupState.COMPLETED, stateEntry.State);
            Assert.Equal(100, stateEntry.Progress);
        }

        [Fact]
        public void FromBackupJob_WithErrorJob_SetsErrorState()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.MarkAsError();

            // Act
            var stateEntry = StateEntry.FromBackupJob(job);

            // Assert
            Assert.Equal(BackupState.ERROR, stateEntry.State);
            Assert.Equal(0, stateEntry.Progress);
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithDefaults()
        {
            // Arrange & Act
            var stateEntry = new StateEntry
            {
                JobName = "TestJob",
                State = BackupState.PENDING
            };

            // Assert
            Assert.Equal("TestJob", stateEntry.JobName);
            Assert.Equal(BackupState.PENDING, stateEntry.State);
        }

        [Theory]
        [InlineData(BackupState.PENDING)]
        [InlineData(BackupState.ACTIVE)]
        [InlineData(BackupState.PAUSED)]
        [InlineData(BackupState.COMPLETED)]
        [InlineData(BackupState.ERROR)]
        public void StateEntry_CanSetAllStates(BackupState state)
        {
            // Arrange & Act
            var entry = new StateEntry { State = state };

            // Assert
            Assert.Equal(state, entry.State);
        }

        [Fact]
        public void Constructor_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var entry = new StateEntry();

            // Assert
            Assert.Equal(string.Empty, entry.JobName);
            Assert.Equal(DateTime.MinValue, entry.Timestamp);
            Assert.Equal(string.Empty, entry.CurrentSourceFile);
            Assert.Equal(string.Empty, entry.CurrentTargetFile);
        }
    }
}