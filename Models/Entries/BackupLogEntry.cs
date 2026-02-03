using System;

namespace Models.Entries
{
    /// <summary>
    /// Represents a single backup log entry
    /// Used for daily log file (logs/YYYY-MM-DD.json)
    /// </summary>
    public class BackupLogEntry
    {
        // ==================== PROPERTIES ====================
        
        /// <summary>
        /// Timestamp of the file transfer
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Name of the backup job
        /// </summary>
        public string BackupName { get; set; }
        
        /// <summary>
        /// Complete source path in UNC format
        /// Example: //server/share/folder/file.txt
        /// </summary>
        public string SourcePath { get; set; }
        
        /// <summary>
        /// Complete target path in UNC format
        /// Example: //backup/share/folder/file.txt
        /// </summary>
        public string TargetPath { get; set; }
        
        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Transfer time in milliseconds
        /// Negative value indicates error
        /// </summary>
        public long TransferTime { get; set; }
        
        // ==================== CONSTRUCTORS ====================
        
        /// <summary>
        /// Parameterless constructor (REQUIRED for JSON/XML serialization)
        /// </summary>
        public BackupLogEntry()
        {
            BackupName = string.Empty;
            SourcePath = string.Empty;
            TargetPath = string.Empty;
            Timestamp = DateTime.Now;
            FileSize = 0;
            TransferTime = 0;
        }
        
        // ==================== HELPER METHODS ====================
        
        /// <summary>
        /// Convert a local path to UNC format
        /// Example: C:\folder\file.txt -> //localhost/C$/folder/file.txt
        /// </summary>
        public static string ToUncPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            
            // Already UNC
            if (path.StartsWith("//") || path.StartsWith(@"\\"))
                return path.Replace('\\', '/');
            
            // Local path with drive letter
            if (path.Length >= 2 && path[1] == ':')
            {
                string drive = path.Substring(0, 1);
                string remainder = path.Substring(2).Replace('\\', '/');
                return $"//localhost/{drive}${remainder}";
            }
            
            return path;
        }
        
        /// <summary>
        /// Check if transfer was successful
        /// </summary>
        public bool IsSuccess()
        {
            return TransferTime >= 0;
        }
        
        /// <summary>
        /// Get human-readable file size
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
        /// Override ToString for debugging
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {BackupName}: {SourcePath} -> {TargetPath} ({GetFormattedSize()}, {TransferTime}ms)";
        }
    }
}