using System;

namespace Models.Entries
{
    /// <summary>
    /// Represents a single backup operation log entry, including file transfer details, timestamps, and transfer status.
    /// </summary>
    public class BackupLogEntry
    {

        /// <summary>
        /// The date and time when the file transfer occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The name of the backup job that performed this transfer.
        /// </summary>
        public string BackupName { get; set; }

        /// <summary>
        /// The complete source file path in UNC format (e.g., //server/share/folder/file.txt).
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// The complete destination file path in UNC format (e.g., //backup/share/folder/file.txt).
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// The size of the transferred file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// The duration of the file transfer in milliseconds; negative values indicate transfer errors.
        /// </summary>
        public long TransferTime { get; set; }

        /// <summary>
        /// The duration of the file encryption in milliseconds; 0 means not encrypted.
        /// </summary>
        public long EncryptionTime { get; set; }

        /// <summary>
        /// Initializes a new instance of BackupLogEntry with default values; required for JSON and XML serialization.
        /// </summary>
        public BackupLogEntry()
        {
            BackupName = string.Empty;
            SourcePath = string.Empty;
            TargetPath = string.Empty;
            Timestamp = DateTime.Now;
            FileSize = 0;
            TransferTime = 0;
            EncryptionTime = 0;
        }

        /// <summary>
        /// Determines whether the file transfer was successful based on the transfer time value.
        /// </summary>
        public bool IsSuccess()
        {
            return TransferTime >= 0;
        }

        /// <summary>
        /// Returns the file size formatted as a human-readable string with appropriate unit (B, KB, MB, GB, TB).
        /// </summary>
        public string GetFormattedSize()
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = FileSize;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Returns a formatted string representation of the backup log entry including timestamp, job name, source, destination, size, and transfer time.
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {BackupName}: {SourcePath} -> {TargetPath} ({GetFormattedSize()}, {TransferTime}ms, {EncryptionTime}ms)";
        }
    }
}