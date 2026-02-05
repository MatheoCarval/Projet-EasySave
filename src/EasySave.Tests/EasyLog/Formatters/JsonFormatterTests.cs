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
    /// Unit tests for the JsonFormatter class, verifying serialization, deserialization, collection handling, and formatting options.
    /// </summary>
    public class JsonFormatterTests
    {
        /// <summary>
        /// Verifies that Format with a valid object successfully serializes it to JSON with camelCase naming convention.
        /// </summary>
        [Fact]
        public void Format_WithValidObject_ReturnsJson()
        {
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

        /// <summary>
        /// Verifies that Format throws ArgumentNullException when given a null object to serialize.
        /// </summary>
        [Fact]
        public void Format_WithNull_ThrowsArgumentNullException()
        {
            var formatter = new JsonFormatter<BackupLogEntry>();

#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => formatter.Format(null));
#pragma warning restore CS8625
        }

        /// <summary>
        /// Verifies that FormatCollection with a valid list serializes all entries into a JSON array format.
        /// </summary>
        [Fact]
        public void FormatCollection_WithValidList_ReturnsJsonArray()
        {
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

        /// <summary>
        /// Verifies that FormatCollection with an empty list returns an empty JSON array.
        /// </summary>
        [Fact]
        public void FormatCollection_WithEmptyList_ReturnsEmptyArray()
        {
            var formatter = new JsonFormatter<BackupLogEntry>();
            var entries = new List<BackupLogEntry>();

            var result = formatter.FormatCollection(entries);

            // Assert
            Assert.Equal("[]", result);
        }

        /// <summary>
        /// Verifies that FormatCollection throws ArgumentNullException when given a null collection.
        /// </summary>
        [Fact]
        public void FormatCollection_WithNull_ThrowsArgumentNullException()
        {
            var formatter = new JsonFormatter<BackupLogEntry>();

#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => formatter.FormatCollection(null));
#pragma warning restore CS8625
        }

        /// <summary>
        /// Verifies that Parse with valid JSON string successfully deserializes it into the correct object with proper values.
        /// </summary>
        [Fact]
        public void Parse_WithValidJson_ReturnsObject()
        {
            var formatter = new JsonFormatter<BackupLogEntry>();
            var json = "{\"backupName\":\"TestBackup\",\"sourcePath\":\"C:\\\\Source\",\"fileSize\":1024}";

            var result = formatter.Parse(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestBackup", result.BackupName);
            Assert.Equal(1024, result.FileSize);
        }

        /// <summary>
        /// Verifies that Parse throws ArgumentException when given null, empty, or whitespace JSON content.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Parse_WithInvalidContent_ThrowsArgumentException(string? content)
        {
            var formatter = new JsonFormatter<BackupLogEntry>();

#pragma warning disable CS8604
            Assert.Throws<ArgumentException>(() => formatter.Parse(content));
#pragma warning restore CS8604
        }

        /// <summary>
        /// Verifies that Parse throws FormatterException when given malformed JSON that cannot be deserialized.
        /// </summary>
        [Fact]
        public void Parse_WithInvalidJson_ThrowsFormatterException()
        {
            var formatter = new JsonFormatter<BackupLogEntry>();
            var invalidJson = "{invalid json}";

            Assert.Throws<FormatterException>(() => formatter.Parse(invalidJson));
        }

        /// <summary>
        /// Verifies that ParseCollection with a valid JSON array deserializes it into a list of objects with correct values.
        /// </summary>
        [Fact]
        public void ParseCollection_WithValidJsonArray_ReturnsList()
        {
            var formatter = new JsonFormatter<BackupLogEntry>();
            var json = "[{\"backupName\":\"Backup1\"},{\"backupName\":\"Backup2\"}]";

            var result = formatter.ParseCollection(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Backup1", result.First().BackupName);
        }

        /// <summary>
        /// Verifies that ParseCollection with null, empty, or whitespace content returns an empty enumerable collection.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ParseCollection_WithEmptyContent_ReturnsEmptyEnumerable(string? content)
        {
            var formatter = new JsonFormatter<BackupLogEntry>();

#pragma warning disable CS8604
            var result = formatter.ParseCollection(content);
#pragma warning restore CS8604

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Verifies that JsonFormatter with prettyPrint enabled formats output with indentation and newlines for readability.
        /// </summary>
        [Fact]
        public void Constructor_WithPrettyPrint_FormatsWithIndentation()
        {
            var formatter = new JsonFormatter<BackupLogEntry>(prettyPrint: true);
            var entry = new BackupLogEntry { BackupName = "Test" };

            var result = formatter.Format(entry);

            // Assert
            Assert.Contains("\n", result); // Has newlines when pretty printed
        }

        /// <summary>
        /// Verifies that JsonFormatter with prettyPrint disabled formats output in compact form without indentation or extra whitespace.
        /// </summary>
        [Fact]
        public void Constructor_WithoutPrettyPrint_FormatsCompact()
        {
            var formatter = new JsonFormatter<BackupLogEntry>(prettyPrint: false);
            var entry = new BackupLogEntry { BackupName = "Test" };

            var result = formatter.Format(entry);

            // Assert
            Assert.DoesNotContain("\n  ", result); // No indentation
        }
    }
}
