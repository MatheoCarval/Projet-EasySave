using EasyLog.Formatters;
using EasyLog.Exceptions;
using Models.Entries;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Tests.EasyLog.Formatters
{
    public class XmlFormatterTests
    {
        [Fact]
        public void Format_WithValidObject_ReturnsXml()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>();
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
            Assert.Contains("<?xml", result);
            Assert.Contains("<BackupLogEntry", result);
            Assert.Contains("TestBackup", result);
        }

        [Fact]
        public void Format_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>();

            // Act & Assert
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => formatter.Format(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void FormatCollection_WithValidList_ReturnsXmlArray()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>();
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
            Assert.Contains("ArrayOfBackupLogEntry", result);
        }

        [Fact]
        public void FormatCollection_WithEmptyList_ReturnsEmptyXmlArray()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>();
            var entries = new List<BackupLogEntry>();

            // Act
            var result = formatter.FormatCollection(entries);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("ArrayOfBackupLogEntry", result);
        }

        [Fact]
        public void FormatCollection_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>();

            // Act & Assert
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => formatter.FormatCollection(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Parse_WithValidXml_ReturnsObject()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>();
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<BackupLogEntry>
  <BackupName>TestBackup</BackupName>
  <FileSize>1024</FileSize>
</BackupLogEntry>";

            // Act
            var result = formatter.Parse(xml);

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
            var formatter = new XmlFormatter<BackupLogEntry>();

            // Act & Assert
#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => formatter.Parse(content));
#pragma warning restore CS8604
        }

        [Fact]
        public void Parse_WithInvalidXml_ThrowsFormatterException()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>();
            var invalidXml = "<invalid xml>";

            // Act & Assert
            Assert.Throws<FormatterException>(() => formatter.Parse(invalidXml));
        }

        [Fact]
        public void ParseCollection_WithValidXmlArray_ReturnsList()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>();
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ArrayOfBackupLogEntry>
  <BackupLogEntry>
    <BackupName>Backup1</BackupName>
  </BackupLogEntry>
  <BackupLogEntry>
    <BackupName>Backup2</BackupName>
  </BackupLogEntry>
</ArrayOfBackupLogEntry>";

            // Act
            var result = formatter.ParseCollection(xml);

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
            var formatter = new XmlFormatter<BackupLogEntry>();

            // Act
#pragma warning disable CS8604
            var result = formatter.ParseCollection(content);
#pragma warning restore CS8604

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Constructor_WithIndent_FormatsWithIndentation()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>(indent: true);
            var entry = new BackupLogEntry { BackupName = "Test" };

            // Act
            var result = formatter.Format(entry);

            // Assert
            Assert.Contains("\n  ", result); // Has indentation
        }

        [Fact]
        public void Constructor_WithoutIndent_FormatsCompact()
        {
            // Arrange
            var formatter = new XmlFormatter<BackupLogEntry>(indent: false);
            var entry = new BackupLogEntry { BackupName = "Test" };

            // Act
            var result = formatter.Format(entry);

            // Assert
            Assert.DoesNotContain("\n  ", result); // No indentation spaces
        }
    }
}
