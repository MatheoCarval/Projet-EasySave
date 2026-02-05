using EasyLog.Abstractions;
using Models.Entries;
using System.Collections.Generic;

namespace EasySave.Tests.Mocks
{
    public class MockLogger : ILogger
    {
        public List<BackupLogEntry> LoggedEntries { get; } = new List<BackupLogEntry>();

        public void Log<T>(T data) where T : class
        {
            if (data is BackupLogEntry entry)
            {
                LoggedEntries.Add(entry);
            }
        }

        public void LogCollection<T>(IEnumerable<T> data) where T : class
        {
            foreach (var item in data)
            {
                Log(item);
            }
        }

        public void Flush()
        {
            // Ne rien faire dans le mock
        }

        public IEnumerable<T> ReadLog<T>() where T : class
        {
            if (typeof(T) == typeof(BackupLogEntry))
            {
                return (IEnumerable<T>)(object)LoggedEntries;
            }
            return new List<T>();
        }

        public void ClearLogs()
        {
            LoggedEntries.Clear();
        }
    }
}