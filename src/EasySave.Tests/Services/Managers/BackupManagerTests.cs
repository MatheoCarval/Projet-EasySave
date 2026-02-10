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
    /// <summary>
    /// Unit tests for the BackupManager class, verifying job creation, deletion, retrieval, execution, and parameter validation.
    /// </summary>
    public class BackupManagerTests : IDisposable
    {
        /// <summary>
        /// Mock logger instance used for dependency injection into BackupManager and related services.
        /// </summary>
        private readonly Mock<ILogger> _mockLogger;
        /// <summary>
        /// State writer instance for managing backup job state persistence during tests.
        /// </summary>
        private readonly StateWriter _stateWriter;
        /// <summary>
        /// File transfer service instance for managing file copy operations during backup execution tests.
        /// </summary>
        private readonly FileTransferService _fileTransferService;
        /// <summary>
        /// Path to a temporary JSON file used for state management during test execution.
        /// </summary>
        private readonly string _testFilePath;
        /// <summary>
        /// Path to the jobs data file that stores backup job definitions, cleared before each test.
        /// </summary>
        private readonly string _jobsFilePath;

        /// <summary>
        /// Initializes test fixtures including mock logger, state writer, file transfer service, and clears the jobs data file and state file before each test.
        /// </summary>
        public BackupManagerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_state_{Guid.NewGuid()}.json");
            _stateWriter = new StateWriter(_testFilePath);
            _fileTransferService = new FileTransferService(_mockLogger.Object, _stateWriter);
            
            _jobsFilePath = "./Datas/jobs.json";
            if (File.Exists(_jobsFilePath))
            {
                File.Delete(_jobsFilePath);
            }
            
            Directory.CreateDirectory("./Datas");
        }

        /// <summary>
        /// Cleans up temporary test files and the jobs data file after test execution completes.
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
            
            if (File.Exists(_jobsFilePath))
            {
                File.Delete(_jobsFilePath);
            }
        }

        /// <summary>
        /// Verifies that BackupManager constructor with valid parameters initializes successfully and creates an empty job list.
        /// </summary>
        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            Assert.NotNull(manager);
            Assert.Empty(manager.GetAllJobs());
        }

        /// <summary>
        /// Verifies that BackupManager constructor throws ArgumentNullException when passed a null file transfer service parameter.
        /// </summary>
        [Fact]
        public void Constructor_WithNullFileTransferService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BackupManager(null!, _stateWriter));
        }

        /// <summary>
        /// Verifies that BackupManager constructor throws ArgumentNullException when passed a null state writer parameter.
        /// </summary>
        [Fact]
        public void Constructor_WithNullStateWriter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BackupManager(_fileTransferService, null!));
        }

        /// <summary>
        /// Verifies that BackupManager constructor throws ArgumentOutOfRangeException when passed zero as the maximum jobs limit.
        /// </summary>
        [Fact]
        public void Constructor_WithZeroMaxJobs_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BackupManager(_fileTransferService, _stateWriter, 0));
        }

        /// <summary>
        /// Verifies that BackupManager constructor throws ArgumentOutOfRangeException when passed negative value as the maximum jobs limit.
        /// </summary>
        [Fact]
        public void Constructor_WithNegativeMaxJobs_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BackupManager(_fileTransferService, _stateWriter, -1));
        }

        /// <summary>
        /// Verifies that CreateJob with valid parameters successfully creates a new backup job and returns it with correct properties.
        /// </summary>
        [Fact]
        public void CreateJob_WithValidParameters_CreatesJob()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            var job = manager.CreateJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            Assert.NotNull(job);
            Assert.Equal("TestJob", job.Name);
            Assert.Single(manager.GetAllJobs());
        }

        /// <summary>
        /// Verifies that CreateJob throws ArgumentException when attempting to create a job with a name that already exists.
        /// </summary>
        [Fact]
        public void CreateJob_WithDuplicateName_ThrowsArgumentException()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);
            manager.CreateJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            var exception = Assert.Throws<ArgumentException>(() =>
                manager.CreateJob("TestJob", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.COMPLETE));
            Assert.Contains("already exists", exception.Message);
        }


        /// <summary>
        /// Verifies that DeleteJob successfully removes an existing job and returns true.
        /// </summary>
        [Fact]
        public void DeleteJob_WithExistingJob_ReturnsTrue()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);
            var job = manager.CreateJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            var result = manager.DeleteJob(job.Id);

            Assert.True(result);
            Assert.Empty(manager.GetAllJobs());
        }

        /// <summary>
        /// Verifies that DeleteJob returns false when attempting to delete a job that does not exist.
        /// </summary>
        [Fact]
        public void DeleteJob_WithNonExistingJob_ReturnsFalse()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            var result = manager.DeleteJob("NonExistent");

            Assert.False(result);
        }

        /// <summary>
        /// Verifies that DeleteJob throws ArgumentException when given null, empty, or whitespace job identifiers.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void DeleteJob_WithInvalidJobId_ThrowsArgumentException(string? jobId)
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);

#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => manager.DeleteJob(jobId));
#pragma warning restore CS8604
        }

        /// <summary>
        /// Verifies that GetJobByName retrieves and returns the correct job when a matching job exists.
        /// </summary>
        [Fact]
        public void GetJob_WithExistingJob_ReturnsJob()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);
            manager.CreateJob("TestJob", new List<string> { @"C:\Source" }, @"C:\Target", BackupType.COMPLETE);

            var job = manager.GetJobByName("TestJob");

            Assert.NotNull(job);
            Assert.Equal("TestJob", job.Name);
        }

        /// <summary>
        /// Verifies that GetJobByName returns null when no matching job exists.
        /// </summary>
        [Fact]
        public void GetJob_WithNonExistingJob_ReturnsNull()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            var job = manager.GetJobByName("NonExistent");

            Assert.Null(job);
        }

        /// <summary>
        /// Verifies that GetJob throws ArgumentException when given null, empty, or whitespace job identifiers.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GetJob_WithInvalidJobId_ThrowsArgumentException(string? jobId)
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);

#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => manager.GetJob(jobId));
#pragma warning restore CS8604
        }

        /// <summary>
        /// Verifies that GetAllJobs returns all created backup jobs in the collection.
        /// </summary>
        [Fact]
        public void GetAllJobs_ReturnsAllJobs()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);
            manager.CreateJob("Job1", new List<string> { @"C:\Source1" }, @"C:\Target1", BackupType.COMPLETE);
            manager.CreateJob("Job2", new List<string> { @"C:\Source2" }, @"C:\Target2", BackupType.DIFFERENTIAL);

            var jobs = manager.GetAllJobs();

            Assert.Equal(2, jobs.Count);
            Assert.Contains(jobs, j => j.Name == "Job1");
            Assert.Contains(jobs, j => j.Name == "Job2");
        }

        /// <summary>
        /// Verifies that GetAllJobs returns an empty collection when no jobs have been created.
        /// </summary>
        [Fact]
        public void GetAllJobs_ReturnsEmptyList_WhenNoJobs()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            var jobs = manager.GetAllJobs();

            Assert.Empty(jobs);
        }

        /// <summary>
        /// Verifies that ExecuteJob throws ArgumentException when attempting to execute a job that does not exist.
        /// </summary>
        [Fact]
        public void ExecuteJob_WithNonExistingJob_ThrowsArgumentException()
        {
            var manager = new BackupManager(_fileTransferService, _stateWriter);

            var exception = Assert.Throws<ArgumentException>(() => manager.ExecuteJob("NonExistent"));
            Assert.Contains("does not exist", exception.Message);
        }
    }
}