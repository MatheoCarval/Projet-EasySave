using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Models.Entries;

namespace EasySave.ViewModels
{
    /// <summary>
    /// ViewModel for the LogsVisualizerWindow — provides log entries, state jobs, and selected job JSON.
    /// Follows the same MVVM pattern as MainViewModel: data and business logic live here,
    /// the View only handles UI-specific event routing.
    /// </summary>
    internal class LogsVisualizerViewModel : ViewModelBase
    {
        private string _selectedJobJson = string.Empty;
        private bool _hasSelectedJob;

        /// <summary>
        /// Initializes a new instance of LogsVisualizerViewModel and loads data from disk.
        /// </summary>
        public LogsVisualizerViewModel()
        {
            LogEntries = new ObservableCollection<LogEntryDisplay>();
            StateJobs = new ObservableCollection<StateJobDisplay>();

            LoadLogEntries();
            LoadStateJobs();
        }

        #region Properties

        /// <summary>
        /// Log entries displayed in the Journal tab.
        /// </summary>
        public ObservableCollection<LogEntryDisplay> LogEntries { get; }

        /// <summary>
        /// State job names displayed in the État tab sidebar.
        /// </summary>
        public ObservableCollection<StateJobDisplay> StateJobs { get; }

        /// <summary>
        /// The formatted JSON content of the currently selected state job.
        /// </summary>
        public string SelectedJobJson
        {
            get => _selectedJobJson;
            set => SetProperty(ref _selectedJobJson, value);
        }

        /// <summary>
        /// Whether a state job is currently selected (controls placeholder vs JSON display).
        /// </summary>
        public bool HasSelectedJob
        {
            get => _hasSelectedJob;
            set => SetProperty(ref _hasSelectedJob, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Selects a job by name and loads its JSON content from state.json.
        /// Called from the code-behind when a job item is clicked.
        /// </summary>
        public void SelectJob(string jobName)
        {
            var stateFilePath = GetStateFilePath();
            if (!File.Exists(stateFilePath)) return;

            try
            {
                var json = File.ReadAllText(stateFilePath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(jobName, out var jobElement))
                {
                    SelectedJobJson = JsonSerializer.Serialize(jobElement,
                        new JsonSerializerOptions { WriteIndented = true });
                    HasSelectedJob = true;
                }
            }
            catch { /* state.json might be locked or malformed */ }
        }

        /// <summary>
        /// Loads state job names from state.json into the StateJobs collection.
        /// </summary>
        private void LoadStateJobs()
        {
            StateJobs.Clear();

            var stateFilePath = GetStateFilePath();
            if (!File.Exists(stateFilePath)) return;

            try
            {
                var json = File.ReadAllText(stateFilePath);
                if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}") return;

                using var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    StateJobs.Add(new StateJobDisplay { JobName = prop.Name });
                }
            }
            catch { /* state.json might be malformed or locked */ }
        }

        /// <summary>
        /// Loads log entries from the most recent log file into the LogEntries collection.
        /// </summary>
        private void LoadLogEntries()
        {
            LogEntries.Clear();

            var logsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "logs");

            if (!Directory.Exists(logsDir)) return;

            var logFile = Directory.GetFiles(logsDir, "jobs_*.json")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (logFile == null) return;

            try
            {
                var json = File.ReadAllText(logFile);
                var entries = JsonSerializer.Deserialize<BackupLogEntry[]>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (entries == null) return;

                foreach (var entry in entries)
                {
                    LogEntries.Add(new LogEntryDisplay
                    {
                        Timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        BackupName = entry.BackupName,
                        SourcePath = entry.SourcePath,
                        TargetPath = entry.TargetPath,
                        FileSizeDisplay = entry.GetFormattedSize(),
                        TransferTimeDisplay = $"{entry.TransferTime} ms"
                    });
                }
            }
            catch { /* Log file might be malformed */ }
        }

        /// <summary>
        /// Returns the path to the EasySave state.json file in AppData.
        /// </summary>
        private static string GetStateFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "state.json");
        }

        #endregion
    }

    /// <summary>
    /// Display model for a log entry in the Journal tab.
    /// </summary>
    public class LogEntryDisplay
    {
        public string Timestamp { get; set; } = string.Empty;
        public string BackupName { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public string FileSizeDisplay { get; set; } = string.Empty;
        public string TransferTimeDisplay { get; set; } = string.Empty;
    }

    /// <summary>
    /// Display model for a state job entry in the État tab.
    /// </summary>
    public class StateJobDisplay
    {
        public string JobName { get; set; } = string.Empty;
    }
}
