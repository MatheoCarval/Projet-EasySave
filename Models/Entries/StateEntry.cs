using System;

namespace Models.Entries
{
    /// <summary>
    /// Value object used to serialize/emit the runtime state of a backup job.
    /// </summary>
    public class StateEntry
    {
        /// <summary>
        /// The name of the backup job.
        /// </summary>
        public string JobName { get; set; }
        /// <summary>
        /// The timestamp when this state snapshot was captured.
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// The current state of the backup job.
        /// </summary>
        public global::Models.Enums.BackupState State { get; set; }
        /// <summary>
        /// The total number of files in the backup job.
        /// </summary>
        public long TotalFiles { get; set; }
        /// <summary>
        /// The total size in bytes of all files in the backup job.
        /// </summary>
        public long TotalSize { get; set; }
        /// <summary>
        /// The backup progress expressed as a percentage from 0 to 100.
        /// </summary>
        public int Progress { get; set; }
        /// <summary>
        /// The number of files remaining to be backed up.
        /// </summary>
        public long RemainingFiles { get; set; }
        /// <summary>
        /// The total size in bytes of files remaining to be backed up.
        /// </summary>
        public long RemainingSize { get; set; }
        /// <summary>
        /// The full path of the current source file being processed.
        /// </summary>
        public string CurrentSourceFile { get; set; }
        /// <summary>
        /// The full path of the current destination file being processed.
        /// </summary>
        public string CurrentTargetFile { get; set; }

        /// <summary>
        /// Initializes a new instance of the StateEntry class with default values for all properties.
        /// </summary>
        public StateEntry()
        {
            JobName = string.Empty;
            Timestamp = DateTime.MinValue;
            CurrentSourceFile = string.Empty;
            CurrentTargetFile = string.Empty;
        }

        /// <summary>
        /// Creates a StateEntry snapshot from a BackupJob instance, capturing its current state and progress information.
        /// </summary>
        public static StateEntry FromBackupJob(global::Models.BackupJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            return new StateEntry
            {
                JobName = job.Name ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                State = job.BackupState,
                TotalFiles = job.TotalFiles,
                TotalSize = job.TotalSize,
                Progress = (int)Math.Clamp(Math.Round(job.Progress), 0, 100),
                RemainingFiles = job.RemainingFiles,
                RemainingSize = job.RemainingSize,
                CurrentSourceFile = job.CurrentSourceFile ?? string.Empty,
                CurrentTargetFile = job.CurrentTargetFile ?? string.Empty
            };
        }
    }
}