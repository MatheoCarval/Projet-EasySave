namespace Models.Enums
{
    /// <summary>
    /// Determines where backup log entries are persisted.
    /// </summary>
    public enum LogStorageMode
    {
        /// <summary>Logs written to local files only (default). No remote server required.</summary>
        Local = 0,

        /// <summary>Logs sent to the remote (Docker) server only. Falls back to local if server unreachable.</summary>
        Remote = 1,

        /// <summary>Logs written to both local files and the remote server.</summary>
        Both = 2
    }
}
