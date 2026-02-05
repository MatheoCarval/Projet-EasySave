using Services.Writers;
using Models;
using Models.Enums;
using Xunit;
using System;
using System.IO;

namespace EasySave.Tests.Services.Writers
{
    /// <summary>
    /// Unit tests for the StateWriter class, verifying state persistence, retrieval, removal, and file management operations.
    /// </summary>
    public class StateWriterTests : IDisposable
    {
        /// <summary>
        /// Path to a temporary JSON file used for testing state persistence operations.
        /// </summary>
        private readonly string _testFilePath;

        /// <summary>
        /// Initializes test fixtures by creating a unique temporary file path for state file testing.
        /// </summary>
        public StateWriterTests()
        {
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_state_{Guid.NewGuid()}.json");
        }

        /// <summary>
        /// Cleans up temporary test files after test execution completes.
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        /// <summary>
        /// Verifies that StateWriter constructor creates the state file at the specified path.
        /// </summary>
        [Fact]
        public void Constructor_WithValidPath_CreatesFile()
        {
            var writer = new StateWriter(_testFilePath);

            Assert.True(File.Exists(_testFilePath));
        }

        /// <summary>
        /// Verifies that StateWriter constructor throws ArgumentNullException when given null, empty, or whitespace file paths.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidPath_ThrowsArgumentNullException(string? path)
        {
#pragma warning disable CS8604
            Assert.Throws<ArgumentNullException>(() => new StateWriter(path));
#pragma warning restore CS8604
        }

        /// <summary>
        /// Verifies that UpdateJobState writes the backup job state to the state file correctly.
        /// </summary>
        [Fact]
        public void UpdateJobState_WithValidJob_WritesToFile()
        {
            var writer = new StateWriter(_testFilePath);
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            writer.UpdateJobState(job);

            var content = File.ReadAllText(_testFilePath);
            Assert.Contains("TestJob", content);
        }

        /// <summary>
        /// Verifies that UpdateJobState throws ArgumentNullException when passed a null job parameter.
        /// </summary>
        [Fact]
        public void UpdateJobState_WithNullJob_ThrowsArgumentNullException()
        {
            var writer = new StateWriter(_testFilePath);

            Assert.Throws<ArgumentNullException>(() => writer.UpdateJobState(null!));
        }

        /// <summary>
        /// Verifies that GetJobState retrieves the correct state entry for an existing job from the state file.
        /// </summary>
        [Fact]
        public void GetJobState_WithExistingJob_ReturnsState()
        {
            var writer = new StateWriter(_testFilePath);
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            writer.UpdateJobState(job);

            var state = writer.GetJobState("TestJob");

            Assert.NotNull(state);
            Assert.Equal("TestJob", state.JobName);
        }

        /// <summary>
        /// Verifies that GetJobState returns null when requesting state for a job that does not exist.
        /// </summary>
        [Fact]
        public void GetJobState_WithNonExistingJob_ReturnsNull()
        {
            var writer = new StateWriter(_testFilePath);

            var state = writer.GetJobState("NonExistent");

            Assert.Null(state);
        }

        /// <summary>
        /// Verifies that GetJobState throws ArgumentNullException when given null, empty, or whitespace job names.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GetJobState_WithInvalidJobName_ThrowsArgumentNullException(string? jobName)
        {
            var writer = new StateWriter(_testFilePath);

#pragma warning disable CS8604
            Assert.Throws<ArgumentNullException>(() => writer.GetJobState(jobName));
#pragma warning restore CS8604
        }

        /// <summary>
        /// Verifies that RemoveJobState successfully removes an existing job state and returns true.
        /// </summary>
        [Fact]
        public void RemoveJobState_WithExistingJob_ReturnsTrue()
        {
            var writer = new StateWriter(_testFilePath);
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            writer.UpdateJobState(job);

            var result = writer.RemoveJobState("TestJob");

            Assert.True(result);
            Assert.Null(writer.GetJobState("TestJob"));
        }

        /// <summary>
        /// Verifies that RemoveJobState returns false when attempting to remove a job state that does not exist.
        /// </summary>
        [Fact]
        public void RemoveJobState_WithNonExistingJob_ReturnsFalse()
        {
            var writer = new StateWriter(_testFilePath);

            var result = writer.RemoveJobState("NonExistent");

            Assert.False(result);
        }

        /// <summary>
        /// Verifies that RemoveJobState throws ArgumentNullException when given null, empty, or whitespace job names.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void RemoveJobState_WithInvalidJobName_ThrowsArgumentNullException(string? jobName)
        {
            var writer = new StateWriter(_testFilePath);

#pragma warning disable CS8604
            Assert.Throws<ArgumentNullException>(() => writer.RemoveJobState(jobName));
#pragma warning restore CS8604
        }

        /// <summary>
        /// Verifies that GetCurrentState retrieves all job states currently stored in the state file.
        /// </summary>
        [Fact]
        public void GetCurrentState_ReturnsAllStates()
        {
            var writer = new StateWriter(_testFilePath);
            var job1 = new BackupJob("Job1", new List<string> { @"C:\Source1" }, @"C:\Target1", BackupType.COMPLETE);
            var job2 = new BackupJob("Job2", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.DIFFERENTIAL);
            writer.UpdateJobState(job1);
            writer.UpdateJobState(job2);

            var states = writer.GetCurrentState();

            Assert.Equal(2, states.Count);
            Assert.Contains("Job1", states.Keys);
            Assert.Contains("Job2", states.Keys);
        }

        /// <summary>
        /// Verifies that ClearAllStates removes all job state entries from the state file.
        /// </summary>
        [Fact]
        public void ClearAllStates_RemovesAllStates()
        {
            var writer = new StateWriter(_testFilePath);
            var job1 = new BackupJob("Job1", new List<string> { @"C:\Source1" }, @"C:\Target1", BackupType.COMPLETE);
            var job2 = new BackupJob("Job2", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.DIFFERENTIAL);
            writer.UpdateJobState(job1);
            writer.UpdateJobState(job2);

            writer.ClearAllStates();

            var states = writer.GetCurrentState();
            Assert.Empty(states);
        }

        /// <summary>
        /// Verifies that calling UpdateJobState multiple times with modified job properties correctly updates the persisted state.
        /// </summary>
        [Fact]
        public void UpdateJobState_MultipleTimes_UpdatesState()
        {
            var writer = new StateWriter(_testFilePath);
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            
            writer.UpdateJobState(job);
            job.TotalFiles = 100;
            writer.UpdateJobState(job);
            job.Progress = 50;
            writer.UpdateJobState(job);

            var state = writer.GetJobState("TestJob");
            Assert.NotNull(state);
            Assert.Equal(100, state.TotalFiles);
            Assert.Equal(50, state.Progress);
        }
    }
}