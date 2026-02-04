using System;

namespace Models.Entries
{
    /// <summary>
    /// Value object used to serialize/emit the runtime state of a backup job.
    /// </summary>
    public class StateEntry
    {
        public string JobName { get; set; }
        public DateTime Timestamp { get; set; }
        public global::Models.Enums.BackupState State { get; set; }
        public long TotalFiles { get; set; }
        public long TotalSize { get; set; }
        /// <summary>
        /// Progress expressed as percentage (0..100).
        /// </summary>
        public int Progress { get; set; }
        public long RemainingFiles { get; set; }
        public long RemainingSize { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }

        public StateEntry()
        {
            JobName = string.Empty;
            Timestamp = DateTime.MinValue;
            CurrentSourceFile = string.Empty;
            CurrentTargetFile = string.Empty;
        }

        /// <summary>
        /// Creates a StateEntry snapshot from a <see cref="Models.BackupJob"/> instance.
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