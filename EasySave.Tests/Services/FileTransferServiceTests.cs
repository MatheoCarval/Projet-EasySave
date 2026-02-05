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
    public class FileTransferServiceTests : IDisposable
    {
        private readonly MockLogger _mockLogger;
        private readonly StateWriter _stateWriter;
        private readonly FileTransferService _service;
        private readonly string _tempDir;
        private readonly string _sourceDir;
        private readonly string _targetDir;
        private readonly string _stateFile;

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

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch { /* Ignorer les erreurs de nettoyage */ }
            }
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new FileTransferService(null!, _stateWriter));
        }

        [Fact]
        public void Constructor_WithNullStateWriter_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new FileTransferService(_mockLogger, null!));
        }

        [Fact]
        public void TransferDirectory_WithNonExistentSource_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            var job = new BackupJob("TestJob", new List<string> { @"C:\NonExistent" }, _targetDir, BackupType.COMPLETE);

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => 
                _service.TransferDirectory(@"C:\NonExistent", _targetDir, job));
        }

        [Fact]
        public void TransferDirectory_WithFileAsSource_ThrowsInvalidOperationException()
        {
            // Arrange
            var filePath = Path.Combine(_sourceDir, "test.txt");
            File.WriteAllText(filePath, "test");
            var job = new BackupJob("TestJob", new List<string> { filePath }, _targetDir, BackupType.COMPLETE);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _service.TransferDirectory(filePath, _targetDir, job));
        }

        [Fact]
        public void TransferDirectory_WithCompleteBackup_CopiesAllFiles()
        {
            // Arrange
            CreateTestFiles(_sourceDir, 3);
            var job = new BackupJob("TestJob", new List<string> { _sourceDir }, _targetDir, BackupType.COMPLETE);

            // Act
            _service.TransferDirectory(_sourceDir, _targetDir, job);

            // Assert
            Assert.Equal(BackupState.COMPLETED, job.BackupState);
            Assert.Equal(100, job.Progress);
            Assert.Equal(0, job.RemainingFiles);
            Assert.Equal(3, Directory.GetFiles(_targetDir).Length);
        }

        [Fact]
        public void TransferFile_WithValidFile_CopiesSuccessfully()
        {
            // Arrange
            var sourceFile = Path.Combine(_sourceDir, "test.txt");
            var targetFile = Path.Combine(_targetDir, "test.txt");
            File.WriteAllText(sourceFile, "test content");
            var job = new BackupJob("TestJob", new List<string> { _sourceDir }, _targetDir, BackupType.COMPLETE);
            job.TotalFiles = 1;
            job.RemainingFiles = 1;

            // Act
            _service.TransferFile(sourceFile, targetFile, job);

            // Assert
            Assert.True(File.Exists(targetFile));
            Assert.Equal("test content", File.ReadAllText(targetFile));
            Assert.Equal(0, job.RemainingFiles);
            Assert.Single(_mockLogger.LoggedEntries);
        }

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