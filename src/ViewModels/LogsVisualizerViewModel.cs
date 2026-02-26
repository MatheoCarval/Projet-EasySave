using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;
using Avalonia.Threading;
using EasySave.Services;
using EasySave.Services.Managers;
using Models.Entries;
using Models.Enums;

namespace EasySave.ViewModels
{
    /// <summary>
    /// ViewModel for the LogsVisualizerWindow.
    /// Loads local logs from disk and (when configured) remote logs from the server.
    /// </summary>
    internal class LogsVisualizerViewModel : ViewModelBase
    {
        private string _selectedJobJson = string.Empty;
        private bool _hasSelectedJob;
        private bool _isLoadingRemote;
        private string _localStatusMessage = string.Empty;
        private string _remoteStatusMessage = string.Empty;
        private int _sourceFilter = 0; // 0=All, 1=Local, 2=Remote

        // Full backing list — filtered into LogEntries
        private readonly List<LogEntryDisplay> _allEntries = new();

        public LogsVisualizerViewModel()
        {
            LogEntries = new ObservableCollection<LogEntryDisplay>();
            StateJobs  = new ObservableCollection<StateJobDisplay>();

            FilterAllCommand    = new RelayCommand(() => SourceFilter = 0);
            FilterLocalCommand  = new RelayCommand(() => SourceFilter = 1);
            FilterRemoteCommand = new RelayCommand(() => SourceFilter = 2);
            RefreshCommand      = new RelayCommand(() => _ = ReloadAsync());

            LoadLogEntries();
            LoadStateJobs();

            _ = LoadRemoteLogsAsync();
        }

        #region Properties

        public ObservableCollection<LogEntryDisplay> LogEntries { get; }
        public ObservableCollection<StateJobDisplay> StateJobs  { get; }

        public string SelectedJobJson
        {
            get => _selectedJobJson;
            set => SetProperty(ref _selectedJobJson, value);
        }

        public bool HasSelectedJob
        {
            get => _hasSelectedJob;
            set => SetProperty(ref _hasSelectedJob, value);
        }

        public bool IsLoadingRemote
        {
            get => _isLoadingRemote;
            set => SetProperty(ref _isLoadingRemote, value);
        }

        public string RemoteStatusMessage
        {
            get => _remoteStatusMessage;
            set => SetProperty(ref _remoteStatusMessage, value);
        }

        public string LocalStatusMessage
        {
            get => _localStatusMessage;
            set => SetProperty(ref _localStatusMessage, value);
        }

        // ── Source filter ───────────────────────────────────────────────────

        public int SourceFilter
        {
            get => _sourceFilter;
            set
            {
                if (SetProperty(ref _sourceFilter, value))
                {
                    OnPropertyChanged(nameof(IsFilterAll));
                    OnPropertyChanged(nameof(IsFilterLocal));
                    OnPropertyChanged(nameof(IsFilterRemote));
                    ApplyFilter();
                }
            }
        }

        public bool IsFilterAll    => _sourceFilter == 0;
        public bool IsFilterLocal  => _sourceFilter == 1;
        public bool IsFilterRemote => _sourceFilter == 2;

        /// <summary>True when a remote server URL + key are configured (not Local-only mode).</summary>
        public bool IsRemoteConfigured
        {
            get
            {
                var config = ConfigurationManager.GetInstance().LoadConfiguration();
                return config.GetLogStorageMode() != LogStorageMode.Local
                    && !string.IsNullOrWhiteSpace(config.GetRemoteLoggingUrl())
                    && !string.IsNullOrWhiteSpace(config.GetRemoteLoggingApiKey());
            }
        }

        #endregion

        #region Commands

        public ICommand FilterAllCommand    { get; }
        public ICommand FilterLocalCommand  { get; }
        public ICommand FilterRemoteCommand { get; }
        public ICommand RefreshCommand      { get; }

        #endregion

        #region Public methods

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
            catch { }
        }

        #endregion

        #region Private helpers

        private async Task ReloadAsync()
        {
            _allEntries.Clear();
            LoadLogEntries();
            await LoadRemoteLogsAsync();
        }

        private void LoadLogEntries()
        {
            var config = ConfigurationManager.GetInstance().LoadConfiguration();
            var configuredPath = config.GetLogFilePath();
            var logFile = ResolveLatestLocalLogFile(configuredPath);

            if (logFile == null)
            {
                var shownPath = string.IsNullOrWhiteSpace(configuredPath)
                    ? "(vide — chemin par défaut utilisé)"
                    : configuredPath;
                LocalStatusMessage = $"Aucun log local journalier trouvé. Chemin configuré : {shownPath}";
                ApplyFilter();
                return;
            }

            try
            {
                var entries = ReadEntriesFromLogFile(logFile).ToList();

                foreach (var entry in entries)
                    _allEntries.Add(new LogEntryDisplay
                    {
                        Timestamp          = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        BackupName         = entry.BackupName,
                        SourcePath         = entry.SourcePath,
                        TargetPath         = entry.TargetPath,
                        FileSizeDisplay    = entry.GetFormattedSize(),
                        TransferTimeDisplay = $"{entry.TransferTime} ms",
                        IsRemote           = false
                    });

                LocalStatusMessage = entries.Count > 0
                    ? $"Logs locaux : {entries.Count} entrée(s) chargée(s) depuis {Path.GetFileName(logFile)}"
                    : $"Fichier local trouvé mais vide : {Path.GetFileName(logFile)}";
            }
            catch (Exception ex)
            {
                LocalStatusMessage = $"Erreur lecture logs locaux : {ex.Message}";
            }

            ApplyFilter();
        }

        private static string? ResolveLatestLocalLogFile(string configuredLogPath)
        {
            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(configuredLogPath))
            {
                var path = configuredLogPath.Trim();
                var isDirectoryPath = Directory.Exists(path)
                    || (!File.Exists(path) && !Path.HasExtension(path));

                if (isDirectoryPath)
                {
                    if (Directory.Exists(path))
                    {
                        candidates.AddRange(Directory.GetFiles(path, "jobs_*.json"));
                        candidates.AddRange(Directory.GetFiles(path, "jobs_*.xml"));
                    }
                }
                else
                {
                    var directory = Path.GetDirectoryName(path);
                    var baseName = Path.GetFileNameWithoutExtension(path);
                    var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

                    if (!string.IsNullOrWhiteSpace(directory)
                        && Directory.Exists(directory)
                        && !string.IsNullOrWhiteSpace(baseName))
                    {
                        if (extension == "json" || extension == "xml")
                        {
                            candidates.AddRange(Directory.GetFiles(directory, $"{baseName}_*.{extension}"));
                        }
                        else
                        {
                            candidates.AddRange(Directory.GetFiles(directory, $"{baseName}_*.json"));
                            candidates.AddRange(Directory.GetFiles(directory, $"{baseName}_*.xml"));
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                var fallbackDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EasySave", "logs");

                if (Directory.Exists(fallbackDir))
                {
                    candidates.AddRange(Directory.GetFiles(fallbackDir, "jobs_*.json"));
                    candidates.AddRange(Directory.GetFiles(fallbackDir, "jobs_*.xml"));
                }
            }

            return candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .FirstOrDefault();
        }

        private static IEnumerable<BackupLogEntry> ReadEntriesFromLogFile(string logFile)
        {
            var extension = Path.GetExtension(logFile).ToLowerInvariant();

            if (extension == ".xml")
            {
                // Read as text first to handle encoding mismatch (UTF-16 declaration in UTF-8 file)
                var xmlText = File.ReadAllText(logFile);
                var serializer = new XmlSerializer(typeof(List<BackupLogEntry>));
                using var reader = new StringReader(xmlText);
                return serializer.Deserialize(reader) as List<BackupLogEntry>
                    ?? Enumerable.Empty<BackupLogEntry>();
            }

            var json = File.ReadAllText(logFile);
            return JsonSerializer.Deserialize<List<BackupLogEntry>>(json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? Enumerable.Empty<BackupLogEntry>();
        }

        private async Task LoadRemoteLogsAsync()
        {
            var config = ConfigurationManager.GetInstance().LoadConfiguration();
            var url    = config.GetRemoteLoggingUrl();
            var key    = config.GetRemoteLoggingApiKey();
            var mode   = config.GetLogStorageMode();

            if (mode == LogStorageMode.Local
                || string.IsNullOrWhiteSpace(url)
                || string.IsNullOrWhiteSpace(key))
                return;

            IsLoadingRemote    = true;
            RemoteStatusMessage = string.Empty;

            var client = new RemoteLogClient(url, key);
            var (entries, error) = await client.GetLogsAsync(pageSize: 200);

            // Deduplicate: skip remote entries whose rawPayload timestamp already exists locally
            var localTimestamps = new HashSet<string>(
                _allEntries.Where(e => !e.IsRemote).Select(e => e.Timestamp));

            foreach (var entry in entries)
            {
                var display = ToDisplayEntry(entry);
                if (!localTimestamps.Contains(display.Timestamp))
                    _allEntries.Add(display);
            }

            IsLoadingRemote = false;

            if (error != null)
                RemoteStatusMessage = $"Logs distants : {error}";
            else if (entries.Count > 0)
                RemoteStatusMessage = $"{entries.Count} log(s) distant(s) chargé(s)";

            ApplyFilter();
        }

        private static LogEntryDisplay ToDisplayEntry(RemoteLogEntry remote)
        {
            if (remote.BackupEntry is { } be)
                return new LogEntryDisplay
                {
                    Timestamp           = (remote.ServerTimestamp ?? be.Timestamp).ToString("yyyy-MM-dd HH:mm:ss"),
                    BackupName          = be.BackupName,
                    SourcePath          = be.SourcePath,
                    TargetPath          = be.TargetPath,
                    FileSizeDisplay     = be.GetFormattedSize(),
                    TransferTimeDisplay = $"{be.TransferTime} ms",
                    IsRemote            = true
                };

            // Fallback when rawPayload isn't a BackupLogEntry
            return new LogEntryDisplay
            {
                Timestamp           = (remote.ServerTimestamp ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss"),
                BackupName          = remote.Message,
                SourcePath          = "-",
                TargetPath          = "-",
                FileSizeDisplay     = "-",
                TransferTimeDisplay = "-",
                IsRemote            = true
            };
        }

        private void ApplyFilter()
        {
            LogEntries.Clear();
            var filtered = _sourceFilter switch
            {
                1 => _allEntries.Where(e => !e.IsRemote),
                2 => _allEntries.Where(e => e.IsRemote),
                _ => _allEntries.AsEnumerable()
            };
            foreach (var e in filtered.OrderByDescending(e => e.Timestamp))
                LogEntries.Add(e);
        }

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
                    StateJobs.Add(new StateJobDisplay { JobName = prop.Name });
            }
            catch { }
        }

        private static string GetStateFilePath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "EasySave", "state.json");

        #endregion
    }

    /// <summary>Display model for one log entry (local or remote).</summary>
    public class LogEntryDisplay
    {
        public string Timestamp           { get; set; } = string.Empty;
        public string BackupName          { get; set; } = string.Empty;
        public string SourcePath          { get; set; } = string.Empty;
        public string TargetPath          { get; set; } = string.Empty;
        public string FileSizeDisplay     { get; set; } = string.Empty;
        public string TransferTimeDisplay { get; set; } = string.Empty;
        public bool   IsRemote            { get; set; }

        // Badge display helpers
        public string SourceLabel    => IsRemote ? "DISTANT"   : "LOCAL";
        public string SourceBadgeBg  => IsRemote ? "#1A2196F3" : "#1A4CAF50";
        public string SourceBadgeFg  => IsRemote ? "#2196F3"   : "#4CAF50";
    }

    /// <summary>Display model for a state job entry in the État tab.</summary>
    public class StateJobDisplay
    {
        public string JobName { get; set; } = string.Empty;
    }
}
