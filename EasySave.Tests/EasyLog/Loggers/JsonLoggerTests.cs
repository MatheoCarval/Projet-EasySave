using EasyLog.Loggers;
using EasyLog.Exceptions;
using Models.Entries;
using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EasySave.Tests.EasyLog.Loggers
{
    public class JsonLoggerTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _testFilePath;

        public JsonLoggerTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"JsonLoggerTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _testFilePath = Path.Combine(_testDirectory, "test.json");
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        public void Constructor_WithValidPath_CreatesLogger()
        {
            // Arrange & Act
            var logger = new JsonLogger(_testFilePath);

            // Assert
            Assert.NotNull(logger);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidPath_ThrowsArgumentException(string? path)
        {
            // Act & Assert
#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => new JsonLogger(path));
#pragma warning restore CS8604
        }

        [Fact]
        public void Log_WithValidEntry_WritesToFile()
        {
            // Arrange
            var logger = new JsonLogger(_testFilePath);
            var entry = new BackupLogEntry
            {
                BackupName = "TestBackup",
                SourcePath = "C:\\Source",
                FileSize = 1024
            };

            // Act
            logger.Log(entry);
            logger.Flush();

            // Assert
            Assert.True(File.Exists(_testFilePath));
            var content = File.ReadAllText(_testFilePath);
            Assert.Contains("TestBackup", content);
        }

        [Fact]
        public void Log_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var logger = new JsonLogger(_testFilePath);

            // Act & Assert
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => logger.Log<BackupLogEntry>(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void LogCollection_WithValidEntries_WritesToFile()
        {
            // Arrange
            var logger = new JsonLogger(_testFilePath);
            var entries = new List<BackupLogEntry>
            {
                new BackupLogEntry { BackupName = "Backup1" },
                new BackupLogEntry { BackupName = "Backup2" }
            };

            // Act
            logger.LogCollection(entries);
            logger.Flush();

            // Assert
            Assert.True(File.Exists(_testFilePath));
            var content = File.ReadAllText(_testFilePath);
            Assert.Contains("Backup1", content);
            Assert.Contains("Backup2", content);
        }

        [Fact]
        public void LogCollection_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var logger = new JsonLogger(_testFilePath);

            // Act & Assert
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => logger.LogCollection<BackupLogEntry>(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void ReadLog_WithExistingFile_ReturnsEntries()
        {
            // Arrange
            var logger = new JsonLogger(_testFilePath);
            var entry = new BackupLogEntry { BackupName = "TestBackup", FileSize = 100 };
            logger.Log(entry);
            logger.Flush();

            // Act
            var result = logger.ReadLog<BackupLogEntry>();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("TestBackup", result.First().BackupName);
        }

        [Fact]
        public void ReadLog_WithNonExistentFile_ReturnsEmptyList()
        {
            // Arrange
            var logger = new JsonLogger(_testFilePath);

            // Act
            var result = logger.ReadLog<BackupLogEntry>();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Flush_WritesBufferedDataToFile()
        {
            // Arrange
            var logger = new JsonLogger(_testFilePath);
            var entry = new BackupLogEntry { BackupName = "TestBackup" };
            logger.Log(entry);

            // Act
            logger.Flush();

            // Assert
            Assert.True(File.Exists(_testFilePath));
            var content = File.ReadAllText(_testFilePath);
            Assert.Contains("TestBackup", content);
        }

        [Fact]
        public void Log_MultipleTimes_AppendsEntries()
        {
            // Arrange
            var logger = new JsonLogger(_testFilePath);

            // Act
            logger.Log(new BackupLogEntry { BackupName = "Backup1" });
            logger.Flush();
            logger.Log(new BackupLogEntry { BackupName = "Backup2" });
            logger.Flush();

            // Assert
            var result = logger.ReadLog<BackupLogEntry>();
            Assert.Equal(2, result.Count());
        }
    }
}
