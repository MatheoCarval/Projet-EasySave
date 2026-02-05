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
    /// <summary>
    /// Unit tests for the JsonLogger class, verifying JSON serialization, file I/O operations, entry logging, and collection handling.
    /// </summary>
    public class JsonLoggerTests : IDisposable
    {
        /// <summary>
        /// Temporary directory path used for test file storage during test execution.
        /// </summary>
        private readonly string _testDirectory;
        /// <summary>
        /// Path to the test JSON log file created in the temporary directory.
        /// </summary>
        private readonly string _testFilePath;
        private readonly string _dailyFilePath;

        /// <summary>
        /// Initializes test fixtures by creating a temporary directory for test log files.
        /// </summary>
        public JsonLoggerTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"JsonLoggerTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _testFilePath = Path.Combine(_testDirectory, "test.json");
            _dailyFilePath = GetDailyPath(_testFilePath, DateTime.Now);
        }

        /// <summary>
        /// Cleans up temporary test files and directories after test execution completes.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch { }
            }
        }

        /// <summary>
        /// Verifies that JsonLogger constructor successfully creates a logger instance with a valid file path.
        /// </summary>
        [Fact]
        public void Constructor_WithValidPath_CreatesLogger()
        {
            var logger = new JsonLogger(_testFilePath);

            Assert.NotNull(logger);
        }

        /// <summary>
        /// Verifies that Log method writes backup log entries to a JSON file with correct data serialization.
        /// </summary>
        [Fact]
        public void Log_WithValidEntry_WritesToFile()
        {
            var logger = new JsonLogger(_testFilePath);
            var entry = new BackupLogEntry
            {
                BackupName = "TestBackup",
                SourcePath = "C:\\Source",
                FileSize = 1024
            };

            logger.Log(entry);
            logger.Flush();

            Assert.True(File.Exists(_dailyFilePath));
            var content = File.ReadAllText(_dailyFilePath);
            Assert.Contains("TestBackup", content);
        }

        /// <summary>
        /// Verifies that Log method throws ArgumentNullException when passed a null entry parameter.
        /// </summary>
        [Fact]
        public void Log_WithNull_ThrowsArgumentNullException()
        {
            var logger = new JsonLogger(_testFilePath);

#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => logger.Log<BackupLogEntry>(null));
#pragma warning restore CS8625
        }

        /// <summary>
        /// Verifies that LogCollection method writes multiple backup log entries to JSON file with all data preserved.
        /// </summary>
        [Fact]
        public void LogCollection_WithValidEntries_WritesToFile()
        {
            var logger = new JsonLogger(_testFilePath);
            var entries = new List<BackupLogEntry>
            {
                new BackupLogEntry { BackupName = "Backup1" },
                new BackupLogEntry { BackupName = "Backup2" }
            };

            logger.LogCollection(entries);
            logger.Flush();

            Assert.True(File.Exists(_dailyFilePath));
            var content = File.ReadAllText(_dailyFilePath);
            Assert.Contains("Backup1", content);
            Assert.Contains("Backup2", content);
        }

        /// <summary>
        /// Verifies that LogCollection method handles null and empty collections gracefully without throwing exceptions.
        /// </summary>
        [Fact]
        public void LogCollection_WithNull_DoesNotThrow()
        {
            var logger = new JsonLogger(_testFilePath);

#pragma warning disable CS8625
            logger.LogCollection<BackupLogEntry>(null);
#pragma warning restore CS8625
            
            Assert.True(true);
        }

        /// <summary>
        /// Verifies that ReadLog method retrieves previously logged entries from an existing JSON log file.
        /// </summary>
        [Fact]
        public void ReadLog_WithExistingFile_ReturnsEntries()
        {
            var logger = new JsonLogger(_testFilePath);
            var entry = new BackupLogEntry { BackupName = "TestBackup", FileSize = 100 };
            logger.Log(entry);
            logger.Flush();

            var result = logger.ReadLog<BackupLogEntry>();

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("TestBackup", result.First().BackupName);
        }

        /// <summary>
        /// Verifies that ReadLog method returns an empty list when no log file exists.
        /// </summary>
        [Fact]
        public void ReadLog_WithNonExistentFile_ReturnsEmptyList()
        {
            var logger = new JsonLogger(_testFilePath);

            var result = logger.ReadLog<BackupLogEntry>();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Verifies that Flush method writes all buffered log entries to the JSON file immediately.
        /// </summary>
        [Fact]
        public void Flush_WritesBufferedDataToFile()
        {
            var logger = new JsonLogger(_testFilePath);
            var entry = new BackupLogEntry { BackupName = "TestBackup" };
            logger.Log(entry);

            logger.Flush();

            Assert.True(File.Exists(_dailyFilePath));
            var content = File.ReadAllText(_dailyFilePath);
            Assert.Contains("TestBackup", content);
        }

        /// <summary>
        /// Verifies that calling Log multiple times with Flush between operations appends all entries to the JSON file.
        /// </summary>
        [Fact]
        public void Log_MultipleTimes_AppendsEntries()
        {
            var logger = new JsonLogger(_testFilePath);

            logger.Log(new BackupLogEntry { BackupName = "Backup1" });
            logger.Flush();
            logger.Log(new BackupLogEntry { BackupName = "Backup2" });
            logger.Flush();

            var result = logger.ReadLog<BackupLogEntry>();
            Assert.Equal(2, result.Count());
        }

        private static string GetDailyPath(string baseOutputPath, DateTime date)
        {
            string directory = Path.GetDirectoryName(baseOutputPath) ?? string.Empty;
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(baseOutputPath);
            string dateString = date.ToString("yyyy-MM-dd");
            return Path.Combine(directory, $"{filenameWithoutExt}_{dateString}.json");
        }
    }
}
