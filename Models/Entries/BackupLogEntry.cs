using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using FileSystemValidation;

namespace EasySave.Models
{
    [Serializable]
    public class BackupLogEntry
    {

        [JsonPropertyName("timestamp")]
        [XmlElement("Timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("backupName")]
        [XmlElement("BackupName")]
        public string BackupName { get; set; } = string.Empty;

        [JsonPropertyName("sourcePath")]
        [XmlElement("SourcePath")]
        public string SourcePath { get; set; } = string.Empty;

        [JsonPropertyName("targetPath")]
        [XmlElement("TargetPath")]
        public string TargetPath { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        [XmlElement("FileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("transferTime")]
        [XmlElement("TransferTime")]
        public long TransferTime { get; set; }

        [JsonPropertyName("metadata")]
        [XmlElement("Metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        public BackupLogEntry()
        {
            Timestamp = DateTime.Now;
        }

        public BackupLogEntry(string backupName, string sourcePath, string targetPath, long fileSize, long transferTime)
        {
            Timestamp = DateTime.Now;
            BackupName = backupName ?? throw new ArgumentNullException(nameof(backupName));
            SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
            TargetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
            FileSize = fileSize >= 0 ? fileSize : throw new ArgumentOutOfRangeException(nameof(fileSize));
            TransferTime = transferTime >= 0 ? transferTime : throw new ArgumentOutOfRangeException(nameof(transferTime));
        }

        public string ToUncPath()
        {
            return PathValidator.ToUncPath(TargetPath);
        }

        public void SetMetadata(string key, string value)
        {
            Metadata ??= new Dictionary<string, string>();
            Metadata[key] = value;
        }

        public string? GetMetadata(string key)
        {
            return Metadata?.GetValueOrDefault(key);
        }

        public bool HasMetadata(string key)
        {
            return Metadata?.ContainsKey(key) ?? false;
        }
    }
}