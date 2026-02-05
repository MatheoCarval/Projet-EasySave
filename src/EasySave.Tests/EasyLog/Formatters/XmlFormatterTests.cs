using EasyLog.Formatters;
using EasyLog.Exceptions;
using Models.Entries;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Tests.EasyLog.Formatters
{
    /// <summary>
    /// Unit tests for the generic XmlFormatter class, verifying XML serialization, deserialization, collection handling, and indentation options.
    /// </summary>
    public class XmlFormatterTests
    {
        /// <summary>
        /// Verifies that Format method correctly serializes a BackupLogEntry object to well-formed XML with proper declaration and element structure.
        /// </summary>
        [Fact]
        public void Format_WithValidObject_ReturnsXml()
        {
            var formatter = new XmlFormatter<BackupLogEntry>();
            var entry = new BackupLogEntry
            {
                BackupName = "TestBackup",
                SourcePath = "C:\\Source",
                TargetPath = "C:\\Target",
                FileSize = 1024,
                TransferTime = 100
            };

            var result = formatter.Format(entry);

            Assert.NotNull(result);
            Assert.Contains("<?xml", result);
            Assert.Contains("<BackupLogEntry", result);
            Assert.Contains("TestBackup", result);
        }

        /// <summary>
        /// Verifies that Format method throws ArgumentNullException when passed a null object parameter.
        /// </summary>
        [Fact]
        public void Format_WithNull_ThrowsArgumentNullException()
        {
            var formatter = new XmlFormatter<BackupLogEntry>();

#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => formatter.Format(null));
#pragma warning restore CS8625
        }

        /// <summary>
        /// Verifies that FormatCollection method serializes a list of BackupLogEntry objects to an XML array structure with all entries preserved.
        /// </summary>
        [Fact]
        public void FormatCollection_WithValidList_ReturnsXmlArray()
        {
            var formatter = new XmlFormatter<BackupLogEntry>();
            var entries = new List<BackupLogEntry>
            {
                new BackupLogEntry { BackupName = "Backup1", FileSize = 100 },
                new BackupLogEntry { BackupName = "Backup2", FileSize = 200 }
            };

            var result = formatter.FormatCollection(entries);

            Assert.NotNull(result);
            Assert.Contains("Backup1", result);
            Assert.Contains("Backup2", result);
            Assert.Contains("ArrayOfBackupLogEntry", result);
        }

        /// <summary>
        /// Verifies that FormatCollection method produces valid XML array structure even when given an empty list.
        /// </summary>
        [Fact]
        public void FormatCollection_WithEmptyList_ReturnsEmptyXmlArray()
        {
            var formatter = new XmlFormatter<BackupLogEntry>();
            var entries = new List<BackupLogEntry>();

            var result = formatter.FormatCollection(entries);

            Assert.NotNull(result);
            Assert.Contains("ArrayOfBackupLogEntry", result);
        }

        /// <summary>
        /// Verifies that FormatCollection method throws ArgumentNullException when passed a null collection parameter.
        /// </summary>
        [Fact]
        public void FormatCollection_WithNull_ThrowsArgumentNullException()
        {
            var formatter = new XmlFormatter<BackupLogEntry>();

#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => formatter.FormatCollection(null));
#pragma warning restore CS8625
        }

        /// <summary>
        /// Verifies that Parse method correctly deserializes valid XML content into a BackupLogEntry object with all properties populated.
        /// </summary>
        [Fact]
        public void Parse_WithValidXml_ReturnsObject()
        {
            var formatter = new XmlFormatter<BackupLogEntry>();
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<BackupLogEntry>
  <BackupName>TestBackup</BackupName>
  <FileSize>1024</FileSize>
</BackupLogEntry>";

            var result = formatter.Parse(xml);

            Assert.NotNull(result);
            Assert.Equal("TestBackup", result.BackupName);
            Assert.Equal(1024, result.FileSize);
        }

        /// <summary>
        /// Verifies that Parse method throws ArgumentException when given null, empty, or whitespace content parameters.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Parse_WithInvalidContent_ThrowsArgumentException(string? content)
        {
            var formatter = new XmlFormatter<BackupLogEntry>();

#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => formatter.Parse(content));
#pragma warning restore CS8604
        }

        /// <summary>
        /// Verifies that Parse method throws FormatterException when given malformed or invalid XML content.
        /// </summary>
        [Fact]
        public void Parse_WithInvalidXml_ThrowsFormatterException()
        {
            var formatter = new XmlFormatter<BackupLogEntry>();
            var invalidXml = "<invalid xml>";

            Assert.Throws<FormatterException>(() => formatter.Parse(invalidXml));
        }

        /// <summary>
        /// Verifies that ParseCollection method correctly deserializes valid XML array content into a list of BackupLogEntry objects with all entries preserved.
        /// </summary>
        [Fact]
        public void ParseCollection_WithValidXmlArray_ReturnsList()
        {
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

            var result = formatter.ParseCollection(xml);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Backup1", result.First().BackupName);
        }

        /// <summary>
        /// Verifies that ParseCollection method returns an empty enumerable when given null, empty, or whitespace content parameters.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ParseCollection_WithEmptyContent_ReturnsEmptyEnumerable(string? content)
        {
            var formatter = new XmlFormatter<BackupLogEntry>();

#pragma warning disable CS8604
            var result = formatter.ParseCollection(content);
#pragma warning restore CS8604

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Verifies that XmlFormatter constructor with indent=true produces formatted XML output with proper indentation and newlines.
        /// </summary>
        [Fact]
        public void Constructor_WithIndent_FormatsWithIndentation()
        {
            var formatter = new XmlFormatter<BackupLogEntry>(indent: true);
            var entry = new BackupLogEntry { BackupName = "Test" };

            var result = formatter.Format(entry);

            Assert.Contains("\n  ", result);
        }

        /// <summary>
        /// Verifies that XmlFormatter constructor with indent=false produces compact XML output without indentation or unnecessary newlines.
        /// </summary>
        [Fact]
        public void Constructor_WithoutIndent_FormatsCompact()
        {
            var formatter = new XmlFormatter<BackupLogEntry>(indent: false);
            var entry = new BackupLogEntry { BackupName = "Test" };

            var result = formatter.Format(entry);

            Assert.DoesNotContain("\n  ", result);
        }
    }
}
