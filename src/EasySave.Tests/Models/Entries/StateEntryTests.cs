using Models;
using Models.Enums;
using Models.Entries;
using Xunit;

namespace EasySave.Tests.Models.Entries
{
    /// <summary>
    /// Unit tests for the StateEntry class, verifying factory methods, state transitions, and property initialization.
    /// </summary>
    public class StateEntryTests
    {
        /// <summary>
        /// Verifies that FromBackupJob factory method creates a StateEntry with all job properties correctly mapped.
        /// </summary>
        [Fact]
        public void FromBackupJob_CreatesStateEntryCorrectly()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.TotalFiles = 100;
            job.TotalSize = 5000;
            job.RemainingFiles = 50;
            job.RemainingSize = 2500;
            job.Progress = 50;
            job.SetCurrentFile(@"C:\Source\file.txt", @"C:\Target\file.txt");

            var stateEntry = StateEntry.FromBackupJob(job);

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

        /// <summary>
        /// Verifies that FromBackupJob correctly reflects completed state when a job is marked as completed.
        /// </summary>
        [Fact]
        public void FromBackupJob_WithCompletedJob_SetsCompletedState()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.MarkAsCompleted();

            var stateEntry = StateEntry.FromBackupJob(job);

            Assert.Equal(BackupState.COMPLETED, stateEntry.State);
            Assert.Equal(100, stateEntry.Progress);
        }

        /// <summary>
        /// Verifies that FromBackupJob correctly reflects error state when a job is marked with an error.
        /// </summary>
        [Fact]
        public void FromBackupJob_WithErrorJob_SetsErrorState()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            job.MarkAsError();

            var stateEntry = StateEntry.FromBackupJob(job);

            Assert.Equal(BackupState.ERROR, stateEntry.State);
            Assert.Equal(0, stateEntry.Progress);
        }

        /// <summary>
        /// Verifies that StateEntry constructor initializes properties with provided values through object initializer syntax.
        /// </summary>
        [Fact]
        public void Constructor_InitializesPropertiesWithDefaults()
        {
            var stateEntry = new StateEntry
            {
                JobName = "TestJob",
                State = BackupState.PENDING
            };

            Assert.Equal("TestJob", stateEntry.JobName);
            Assert.Equal(BackupState.PENDING, stateEntry.State);
        }

        /// <summary>
        /// Verifies that StateEntry can be assigned all possible BackupState enumeration values.
        /// </summary>
        [Theory]
        [InlineData(BackupState.PENDING)]
        [InlineData(BackupState.ACTIVE)]
        [InlineData(BackupState.PAUSED)]
        [InlineData(BackupState.COMPLETED)]
        [InlineData(BackupState.ERROR)]
        public void StateEntry_CanSetAllStates(BackupState state)
        {
            var entry = new StateEntry { State = state };

            Assert.Equal(state, entry.State);
        }

        /// <summary>
        /// Verifies that StateEntry default constructor initializes properties with appropriate default values.
        /// </summary>
        [Fact]
        public void Constructor_DefaultValues_AreSetCorrectly()
        {
            var entry = new StateEntry();

            Assert.Equal(string.Empty, entry.JobName);
            Assert.Equal(DateTime.MinValue, entry.Timestamp);
            Assert.Equal(string.Empty, entry.CurrentSourceFile);
            Assert.Equal(string.Empty, entry.CurrentTargetFile);
        }
    }
}