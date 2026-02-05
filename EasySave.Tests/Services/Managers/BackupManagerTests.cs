using Services.Managers;
using EasySave.Services;
using Services.Writers;
using Models;
using Models.Enums;
using EasyLog.Abstractions;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.Tests.Services.Managers
{
    public class BackupManagerTests : IDisposable
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly StateWriter _stateWriter;
        private readonly FileTransferService _fileTransferService;
        private readonly string _testFilePath;
        private readonly string _jobsFilePath;

        public BackupManagerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_state_{Guid.NewGuid()}.json");
            _stateWriter = new StateWriter(_testFilePath);
            _fileTransferService = new FileTransferService(_mockLogger.Object, _stateWriter);
            
            // BackupManager loads from ./Datas/jobs.json - clear it before each test
            _jobsFilePath = "./Datas/jobs.json";
            if (File.Exists(_jobsFilePath))
            {
                File.Delete(_jobsFilePath);
            }
            
            // Ensure the Datas directory exists
            Directory.CreateDirectory("./Datas");
        }

        public void Dispose()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
            
            // Clean up jobs file after each test
            if (File.Exists(_jobsFilePath))
            {
                File.Delete(_jobsFilePath);
            }
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            // Assert
            Assert.NotNull(manager);
            Assert.Empty(manager.GetAllJobs());
        }

        [Fact]
        public void Constructor_WithNullFileTransferService_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BackupManager(null!, _stateWriter));
        }

        [Fact]
        public void Constructor_WithNullStateWriter_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BackupManager(_fileTransferService, null!));
        }

        [Fact]
        public void Constructor_WithZeroMaxJobs_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BackupManager(_fileTransferService, _stateWriter, 0));
        }

        [Fact]
        public void Constructor_WithNegativeMaxJobs_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BackupManager(_fileTransferService, _stateWriter, -1));
        }

        [Fact]
        public void CreateJob_WithValidParameters_CreatesJob()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            // Act
            var job = manager.CreateJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            // Assert
            Assert.NotNull(job);
            Assert.Equal("TestJob", job.Name);
            Assert.Single(manager.GetAllJobs());
        }

        [Fact]
        public void CreateJob_WithDuplicateName_ThrowsArgumentException()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);
            manager.CreateJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                manager.CreateJob("TestJob", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.COMPLETE));
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public void CreateJob_WhenMaxJobsReached_ThrowsInvalidOperationException()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter, maxJobs: 2);
            manager.CreateJob("Job1", new List<string> { @"C:\Source1" }, @"C:\Target1", BackupType.COMPLETE);
            manager.CreateJob("Job2", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.COMPLETE);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                manager.CreateJob("Job3", new List<string> { @"C:\Source3" }, @"C:\Target3", BackupType.COMPLETE));
            Assert.Contains("Maximum number of jobs", exception.Message);
        }

        [Fact]
        public void DeleteJob_WithExistingJob_ReturnsTrue()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);
            manager.CreateJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            // Act
            var result = manager.DeleteJob("TestJob");

            // Assert
            Assert.True(result);
            Assert.Empty(manager.GetAllJobs());
        }

        [Fact]
        public void DeleteJob_WithNonExistingJob_ReturnsFalse()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            // Act
            var result = manager.DeleteJob("NonExistent");

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void DeleteJob_WithInvalidJobId_ThrowsArgumentException(string? jobId)
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            // Act & Assert
#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => manager.DeleteJob(jobId));
#pragma warning restore CS8604
        }

        [Fact]
        public void GetJob_WithExistingJob_ReturnsJob()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);
            manager.CreateJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            // Act
            var job = manager.GetJob("TestJob");

            // Assert
            Assert.NotNull(job);
            Assert.Equal("TestJob", job.Name);
        }

        [Fact]
        public void GetJob_WithNonExistingJob_ReturnsNull()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            // Act
            var job = manager.GetJob("NonExistent");

            // Assert
            Assert.Null(job);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GetJob_WithInvalidJobId_ThrowsArgumentException(string? jobId)
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            // Act & Assert
#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => manager.GetJob(jobId));
#pragma warning restore CS8604
        }

        [Fact]
        public void GetAllJobs_ReturnsAllJobs()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);
            manager.CreateJob("Job1", new List<string> { @"C:\Source1" }, @"C:\Target1", BackupType.COMPLETE);
            manager.CreateJob("Job2", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.DIFFERENTIAL);

            // Act
            var jobs = manager.GetAllJobs();

            // Assert
            Assert.Equal(2, jobs.Count);
            Assert.Contains(jobs, j => j.Name == "Job1");
            Assert.Contains(jobs, j => j.Name == "Job2");
        }

        [Fact]
        public void GetAllJobs_ReturnsEmptyList_WhenNoJobs()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            // Act
            var jobs = manager.GetAllJobs();

            // Assert
            Assert.Empty(jobs);
        }

        [Fact]
        public void ExecuteJob_WithNonExistingJob_ThrowsArgumentException()
        {
            // Arrange
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => manager.ExecuteJob("NonExistent"));
            Assert.Contains("does not exist", exception.Message);
        }
    }
}