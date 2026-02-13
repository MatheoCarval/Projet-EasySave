using Models.Entries;
using Xunit;
using System;

namespace EasySave.Tests.Models
{
    /// <summary>
    /// Unit tests for the BackupLogEntry class, verifying property initialization, default values, and value assignment.
    /// </summary>
    public class BackupLogEntryTests
    {
        /// <summary>
        /// Verifies that BackupLogEntry constructor initializes all properties with the specified values.
        /// </summary>
        [Fact]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            var timestamp = DateTime.Now;
            var entry = new BackupLogEntry
            {
                Timestamp = timestamp,
                BackupName = "TestBackup",
                SourcePath = @"C:\Source\file.txt",
                TargetPath = @"C:\Target\file.txt",
                FileSize = 1024,
                TransferTime = 150,
                EncryptionTime = 45
            };

            Assert.Equal(timestamp, entry.Timestamp);
            Assert.Equal("TestBackup", entry.BackupName);
            Assert.Equal(@"C:\Source\file.txt", entry.SourcePath);
            Assert.Equal(@"C:\Target\file.txt", entry.TargetPath);
            Assert.Equal(1024, entry.FileSize);
            Assert.Equal(150, entry.TransferTime);
            Assert.Equal(45, entry.EncryptionTime);
        }

        /// <summary>
        /// Verifies that BackupLogEntry can be instantiated with default values and initializes properties with empty strings and zero values.
        /// </summary>
        [Fact]
        public void BackupLogEntry_CanBeInstantiatedWithDefaultValues()
        {
            var entry = new BackupLogEntry();

            Assert.NotNull(entry);
            Assert.True((DateTime.Now - entry.Timestamp).TotalSeconds < 1);
            Assert.Equal(string.Empty, entry.BackupName);
            Assert.Equal(string.Empty, entry.SourcePath);
            Assert.Equal(string.Empty, entry.TargetPath);
            Assert.Equal(0, entry.FileSize);
            Assert.Equal(0, entry.TransferTime);
            Assert.Equal(0, entry.EncryptionTime);
        }

        /// <summary>
        /// Verifies that FileSize property can be set to different values including zero, standard sizes, and large file sizes.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1024)]
        [InlineData(1048576)]
        public void FileSize_CanBeSetToDifferentValues(long size)
        {
            var entry = new BackupLogEntry { FileSize = size };

            Assert.Equal(size, entry.FileSize);
        }

        /// <summary>
        /// Verifies that TransferTime property can be set to various values including zero, positive durations, and negative values for error states.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(5000)]
        [InlineData(-1)]
        public void TransferTime_CanBeSetToDifferentValues(long time)
        {
            var entry = new BackupLogEntry { TransferTime = time };

            Assert.Equal(time, entry.TransferTime);
        }

        /// <summary>
        /// Verifies that EncryptionTime property can be set to different values including zero and positive durations.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(2500)]
        public void EncryptionTime_CanBeSetToDifferentValues(long time)
        {
            var entry = new BackupLogEntry { EncryptionTime = time };

            Assert.Equal(time, entry.EncryptionTime);
        }
    }
}