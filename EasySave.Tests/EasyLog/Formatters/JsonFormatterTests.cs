using EasyLog.Formatters;
using EasyLog.Exceptions;
using Models.Entries;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Tests.EasyLog.Formatters
{
    public class JsonFormatterTests
    {
        [Fact]
        public void Format_WithValidObject_ReturnsJson()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();
            var entry = new BackupLogEntry
            {
                BackupName = "TestBackup",
                SourcePath = "C:\\Source",
                TargetPath = "C:\\Target",
                FileSize = 1024,
                TransferTime = 100
            };

            // Act
            var result = formatter.Format(entry);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("TestBackup", result);
            Assert.Contains("backupName", result); // camelCase naming policy
        }

        [Fact]
        public void Format_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();

            // Act & Assert
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => formatter.Format(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void FormatCollection_WithValidList_ReturnsJsonArray()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();
            var entries = new List<BackupLogEntry>
            {
                new BackupLogEntry { BackupName = "Backup1", FileSize = 100 },
                new BackupLogEntry { BackupName = "Backup2", FileSize = 200 }
            };

            // Act
            var result = formatter.FormatCollection(entries);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Backup1", result);
            Assert.Contains("Backup2", result);
            Assert.StartsWith("[", result.Trim());
            Assert.EndsWith("]", result.Trim());
        }

        [Fact]
        public void FormatCollection_WithEmptyList_ReturnsEmptyArray()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();
            var entries = new List<BackupLogEntry>();

            // Act
            var result = formatter.FormatCollection(entries);

            // Assert
            Assert.Equal("[]", result);
        }

        [Fact]
        public void FormatCollection_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();

            // Act & Assert
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => formatter.FormatCollection(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Parse_WithValidJson_ReturnsObject()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();
            var json = "{\"backupName\":\"TestBackup\",\"sourcePath\":\"C:\\\\Source\",\"fileSize\":1024}";

            // Act
            var result = formatter.Parse(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestBackup", result.BackupName);
            Assert.Equal(1024, result.FileSize);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Parse_WithInvalidContent_ThrowsArgumentException(string? content)
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();

            // Act & Assert
#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => formatter.Parse(content));
#pragma warning restore CS8604
        }

        [Fact]
        public void Parse_WithInvalidJson_ThrowsFormatterException()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();
            var invalidJson = "{invalid json}";

            // Act & Assert
            Assert.Throws<FormatterException>(() => formatter.Parse(invalidJson));
        }

        [Fact]
        public void ParseCollection_WithValidJsonArray_ReturnsList()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();
            var json = "[{\"backupName\":\"Backup1\"},{\"backupName\":\"Backup2\"}]";

            // Act
            var result = formatter.ParseCollection(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Backup1", result.First().BackupName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ParseCollection_WithEmptyContent_ReturnsEmptyEnumerable(string? content)
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>();

            // Act
#pragma warning disable CS8604
            var result = formatter.ParseCollection(content);
#pragma warning restore CS8604

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Constructor_WithPrettyPrint_FormatsWithIndentation()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>(prettyPrint: true);
            var entry = new BackupLogEntry { BackupName = "Test" };

            // Act
            var result = formatter.Format(entry);

            // Assert
            Assert.Contains("\n", result); // Has newlines when pretty printed
        }

        [Fact]
        public void Constructor_WithoutPrettyPrint_FormatsCompact()
        {
            // Arrange
            var formatter = new JsonFormatter<BackupLogEntry>(prettyPrint: false);
            var entry = new BackupLogEntry { BackupName = "Test" };

            // Act
            var result = formatter.Format(entry);

            // Assert
            Assert.DoesNotContain("\n  ", result); // No indentation
        }
    }
}
