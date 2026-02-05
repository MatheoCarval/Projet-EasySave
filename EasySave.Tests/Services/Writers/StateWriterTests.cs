using Services.Writers;
using Models;
using Models.Enums;
using Xunit;
using System;
using System.IO;

namespace EasySave.Tests.Services.Writers
{
    public class StateWriterTests : IDisposable
    {
        private readonly string _testFilePath;

        public StateWriterTests()
        {
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_state_{Guid.NewGuid()}.json");
        }

        public void Dispose()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [Fact]
        public void Constructor_WithValidPath_CreatesFile()
        {
            // Arrange & Act
            var writer = new StateWriter(_testFilePath);

            // Assert
            Assert.True(File.Exists(_testFilePath));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidPath_ThrowsArgumentNullException(string? path)
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StateWriter(path));
        }

        [Fact]
        public void UpdateJobState_WithValidJob_WritesToFile()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            // Act
            writer.UpdateJobState(job);

            // Assert
            var content = File.ReadAllText(_testFilePath);
            Assert.Contains("TestJob", content);
        }

        [Fact]
        public void UpdateJobState_WithNullJob_ThrowsArgumentNullException()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => writer.UpdateJobState(null!));
        }

        [Fact]
        public void GetJobState_WithExistingJob_ReturnsState()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            writer.UpdateJobState(job);

            // Act
            var state = writer.GetJobState("TestJob");

            // Assert
            Assert.NotNull(state);
            Assert.Equal("TestJob", state.JobName);
        }

        [Fact]
        public void GetJobState_WithNonExistingJob_ReturnsNull()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);

            // Act
            var state = writer.GetJobState("NonExistent");

            // Assert
            Assert.Null(state);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GetJobState_WithInvalidJobName_ThrowsArgumentNullException(string? jobName)
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => writer.GetJobState(jobName));
        }

        [Fact]
        public void RemoveJobState_WithExistingJob_ReturnsTrue()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            writer.UpdateJobState(job);

            // Act
            var result = writer.RemoveJobState("TestJob");

            // Assert
            Assert.True(result);
            Assert.Null(writer.GetJobState("TestJob"));
        }

        [Fact]
        public void RemoveJobState_WithNonExistingJob_ReturnsFalse()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);

            // Act
            var result = writer.RemoveJobState("NonExistent");

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void RemoveJobState_WithInvalidJobName_ThrowsArgumentNullException(string? jobName)
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => writer.RemoveJobState(jobName));
        }

        [Fact]
        public void GetCurrentState_ReturnsAllStates()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);
            var job1 = new BackupJob("Job1", new List<string> { @"C:\Source1" }, @"C:\Target1", BackupType.COMPLETE);
            var job2 = new BackupJob("Job2", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.DIFFERENTIAL);
            writer.UpdateJobState(job1);
            writer.UpdateJobState(job2);

            // Act
            var states = writer.GetCurrentState();

            // Assert
            Assert.Equal(2, states.Count);
            Assert.Contains("Job1", states.Keys);
            Assert.Contains("Job2", states.Keys);
        }

        [Fact]
        public void ClearAllStates_RemovesAllStates()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);
            var job1 = new BackupJob("Job1", new List<string> { @"C:\Source1" }, @"C:\Target1", BackupType.COMPLETE);
            var job2 = new BackupJob("Job2", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.DIFFERENTIAL);
            writer.UpdateJobState(job1);
            writer.UpdateJobState(job2);

            // Act
            writer.ClearAllStates();

            // Assert
            var states = writer.GetCurrentState();
            Assert.Empty(states);
        }

        [Fact]
        public void UpdateJobState_MultipleTimes_UpdatesState()
        {
            // Arrange
            var writer = new StateWriter(_testFilePath);
            var job = new BackupJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);
            
            // Act
            writer.UpdateJobState(job);
            job.TotalFiles = 100;
            writer.UpdateJobState(job);
            job.Progress = 50;
            writer.UpdateJobState(job);

            // Assert
            var state = writer.GetJobState("TestJob");
            Assert.NotNull(state);
            Assert.Equal(100, state.TotalFiles);
            Assert.Equal(50, state.Progress);
        }
    }
}