using EasyLog.Abstractions;
using Models.Entries;
using System.Collections.Generic;

namespace EasySave.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of ILogger for unit testing, capturing logged entries in memory instead of persisting to storage.
    /// </summary>
    public class MockLogger : ILogger
    {
        /// <summary>
        /// Collection of all backup log entries logged through this mock logger instance.
        /// </summary>
        public List<BackupLogEntry> LoggedEntries { get; } = new List<BackupLogEntry>();

        /// <summary>
        /// Logs a single data entry of type T. If the entry is a BackupLogEntry, it is added to the LoggedEntries collection.
        /// </summary>
        public void Log<T>(T data) where T : class
        {
            if (data is BackupLogEntry entry)
            {
                LoggedEntries.Add(entry);
            }
        }

        /// <summary>
        /// Logs a collection of data entries by iterating through each item and calling Log individually.
        /// </summary>
        public void LogCollection<T>(IEnumerable<T> data) where T : class
        {
            foreach (var item in data)
            {
                Log(item);
            }
        }

        /// <summary>
        /// Flushes any buffered data. No operation performed in this mock implementation.
        /// </summary>
        public void Flush()
        {
        }

        /// <summary>
        /// Reads and returns logged entries of type T from the mock logger. Returns BackupLogEntry collection if type matches, otherwise returns empty collection.
        /// </summary>
        public IEnumerable<T> ReadLog<T>() where T : class
        {
            if (typeof(T) == typeof(BackupLogEntry))
            {
                return (IEnumerable<T>)(object)LoggedEntries;
            }
            return new List<T>();
        }

        /// <summary>
        /// Clears all logged entries from the mock logger for test isolation.
        /// </summary>
        public void ClearLogs()
        {
            LoggedEntries.Clear();
        }
    }
}