namespace Models.Enums
{
    /// <summary>
    /// Enumeration representing the different strategies for backup operations.
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// Complete backup copies all files regardless of previous backup state.
        /// </summary>
        COMPLETE,
        /// <summary>
        /// Differential backup copies only files that have changed since the last complete backup.
        /// </summary>
        DIFFERENTIAL
    }
}