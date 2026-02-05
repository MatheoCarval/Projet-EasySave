using EasySave.Services;
using Services.Writers;
using Models;
using Models.Enums;
using Models.Entries;
using EasyLog.Abstractions;
using EasySave.Tests.Mocks;
using Xunit;
using System;
using System.IO;

namespace EasySave.Tests.Services
{
    /// <summary>
    /// Unit tests for the FileTransferService class, verifying file and directory transfer operations, error handling, and state management.
    /// </summary>
    public class FileTransferServiceTests : IDisposable
    {
        /// <summary>
        /// Mock logger instance for capturing logged backup entries during file transfer tests.
        /// </summary>
        private readonly MockLogger _mockLogger;
        /// <summary>
        /// State writer instance for managing backup job state persistence during tests.
        /// </summary>
        private readonly StateWriter _stateWriter;
        /// <summary>
        /// FileTransferService instance under test for file and directory transfer operations.
        /// </summary>
        private readonly FileTransferService _service;
        /// <summary>
        /// Temporary directory created for test execution, containing source and target subdirectories.
        /// </summary>
        private readonly string _tempDir;
        /// <summary>
        /// Source directory path where test files are created for transfer.
        /// </summary>
        private readonly string _sourceDir;
        /// <summary>
        /// Target directory path where test files are copied during transfer tests.
        /// </summary>
        private readonly string _targetDir;
        /// <summary>
        /// State file path for persisting backup job state during tests.
        /// </summary>
        private readonly string _stateFile;

        /// <summary>
        /// Initializes test fixtures including temporary directories, mock logger, state writer, and file transfer service before each test.
        /// </summary>
        public FileTransferServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"FileTransferTest_{Guid.NewGuid()}");
            _sourceDir = Path.Combine(_tempDir, "Source");
            _targetDir = Path.Combine(_tempDir, "Target");
            _stateFile = Path.Combine(_tempDir, "state.json");

            Directory.CreateDirectory(_sourceDir);
            Directory.CreateDirectory(_targetDir);

            _mockLogger = new MockLogger();
            _stateWriter = new StateWriter(_stateFile);
            _service = new FileTransferService(_mockLogger, _stateWriter);
        }

        /// <summary>
        /// Cleans up temporary test directories after test execution completes.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch { }
            }
        }

        /// <summary>
        /// Verifies that FileTransferService constructor throws ArgumentNullException when passed a null logger parameter.
        /// </summary>
        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new FileTransferService(null!, _stateWriter));
        }

        /// <summary>
        /// Verifies that FileTransferService constructor throws ArgumentNullException when passed a null state writer parameter.
        /// </summary>
        [Fact]
        public void Constructor_WithNullStateWriter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new FileTransferService(_mockLogger, null!));
        }

        /// <summary>
        /// Verifies that TransferDirectory throws DirectoryNotFoundException when given a non-existent source path.
        /// </summary>
        [Fact]
        public void TransferDirectory_WithNonExistentSource_ThrowsDirectoryNotFoundException()
        {
            var job = new BackupJob("TestJob", new List<string> { @"C:\NonExistent" }, _targetDir, BackupType.COMPLETE);

            Assert.Throws<DirectoryNotFoundException>(() => 
                _service.TransferDirectory(@"C:\NonExistent", _targetDir, job));
        }

        /// <summary>
        /// Verifies that TransferDirectory throws InvalidOperationException when given a file path instead of a directory as source.
        /// </summary>
        [Fact]
        public void TransferDirectory_WithFileAsSource_ThrowsInvalidOperationException()
        {
            var filePath = Path.Combine(_sourceDir, "test.txt");
            File.WriteAllText(filePath, "test");
            var job = new BackupJob("TestJob", new List<string> { filePath }, _targetDir, BackupType.COMPLETE);

            Assert.Throws<InvalidOperationException>(() => 
                _service.TransferDirectory(filePath, _targetDir, job));
        }

        /// <summary>
        /// Verifies that TransferDirectory successfully copies all files from source to target for a complete backup and updates job progress to completion.
        /// </summary>
        [Fact]
        public void TransferDirectory_WithCompleteBackup_CopiesAllFiles()
        {
            CreateTestFiles(_sourceDir, 3);
            var job = new BackupJob("TestJob", new List<string> { _sourceDir }, _targetDir, BackupType.COMPLETE);
            
            var files = Directory.GetFiles(_sourceDir, "*", SearchOption.AllDirectories);
            job.TotalFiles = files.Length;
            job.RemainingFiles = files.Length;
            job.TotalSize = files.Sum(f => new FileInfo(f).Length);
            job.RemainingSize = job.TotalSize;
            job.BackupState = BackupState.ACTIVE;

            _service.TransferDirectory(_sourceDir, _targetDir, job);

            Assert.Equal(0, job.RemainingFiles);
            Assert.Equal(100, job.Progress);
            Assert.Equal(3, Directory.GetFiles(_targetDir).Length);
        }

        /// <summary>
        /// Verifies that TransferFile successfully copies a file to the target location and logs the transfer operation.
        /// </summary>
        [Fact]
        public void TransferFile_WithValidFile_CopiesSuccessfully()
        {
            var sourceFile = Path.Combine(_sourceDir, "test.txt");
            var targetFile = Path.Combine(_targetDir, "test.txt");
            File.WriteAllText(sourceFile, "test content");
            var job = new BackupJob("TestJob", new List<string> { _sourceDir }, _targetDir, BackupType.COMPLETE);
            job.TotalFiles = 1;
            job.RemainingFiles = 1;

            _service.TransferFile(sourceFile, targetFile, job);

            Assert.True(File.Exists(targetFile));
            Assert.Equal("test content", File.ReadAllText(targetFile));
            Assert.Equal(0, job.RemainingFiles);
            Assert.Single(_mockLogger.LoggedEntries);
        }

        /// <summary>
        /// Creates a specified number of test files in the given directory with sequential naming and content.
        /// </summary>
        private void CreateTestFiles(string directory, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                File.WriteAllText(
                    Path.Combine(directory, $"file{i}.txt"),
                    $"Content of file {i}"
                );
            }
        }
    }
}