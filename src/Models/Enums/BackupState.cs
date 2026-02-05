namespace Models.Enums
{
    /// <summary>
    /// Enumeration representing the various states a backup job can be in during its lifecycle.
    /// </summary>
    public enum BackupState
    {
        /// <summary>
        /// The backup job is currently executing and transferring files.
        /// </summary>
        ACTIVE,
        /// <summary>
        /// The backup job has been temporarily suspended but can be resumed.
        /// </summary>
        PAUSED,
        /// <summary>
        /// The backup job has finished successfully with all files transferred.
        /// </summary>
        COMPLETED,
        /// <summary>
        /// The backup job encountered an error and failed to complete.
        /// </summary>
        ERROR,
        /// <summary>
        /// The backup job is scheduled but has not yet started execution.
        /// </summary>
        PENDING
    }
}