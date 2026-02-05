using Models.Entries;
using Xunit;
using System;

namespace EasySave.Tests.Models
{
    public class BackupLogEntryTests
    {
        [Fact]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            // Arrange
            var timestamp = DateTime.Now;
            var entry = new BackupLogEntry
            {
                Timestamp = timestamp,
                BackupName = "TestBackup",
                SourcePath = @"C:\Source\file.txt",
                TargetPath = @"C:\Target\file.txt",
                FileSize = 1024,
                TransferTime = 150
            };

            // Assert
            Assert.Equal(timestamp, entry.Timestamp);
            Assert.Equal("TestBackup", entry.BackupName);
            Assert.Equal(@"C:\Source\file.txt", entry.SourcePath);
            Assert.Equal(@"C:\Target\file.txt", entry.TargetPath);
            Assert.Equal(1024, entry.FileSize);
            Assert.Equal(150, entry.TransferTime);
        }

        [Fact]
        public void BackupLogEntry_CanBeInstantiatedWithDefaultValues()
        {
            // Arrange & Act
            var entry = new BackupLogEntry();

            // Assert
            Assert.NotNull(entry);
            Assert.Equal(default(DateTime), entry.Timestamp);
            Assert.Null(entry.BackupName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1024)]
        [InlineData(1048576)]
        public void FileSize_CanBeSetToDifferentValues(long size)
        {
            // Arrange
            var entry = new BackupLogEntry { FileSize = size };

            // Assert
            Assert.Equal(size, entry.FileSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(5000)]
        [InlineData(-1)] // Pour les erreurs
        public void TransferTime_CanBeSetToDifferentValues(long time)
        {
            // Arrange
            var entry = new BackupLogEntry { TransferTime = time };

            // Assert
            Assert.Equal(time, entry.TransferTime);
        }
    }
}