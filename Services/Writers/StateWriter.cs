using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Models;
using Models.Entries;
using Models.Enums;

namespace Services.Writers
{
    /// <summary>
    /// Manages and persists real-time state information for all backup jobs, maintaining a single state.json file that is constantly updated during backup operations.
    /// </summary>
    public class StateWriter
    {
        /// <summary>
        /// The file path where backup job state information is persisted.
        /// </summary>
        private readonly string _stateFilePath;
        /// <summary>
        /// In-memory cache of state entries indexed by job name.
        /// </summary>
        private readonly Dictionary<string, StateEntry> _stateEntries;
        /// <summary>
        /// Lock object for thread-safe access to state entries.
        /// </summary>
        private readonly object _lock = new object();
        /// <summary>
        /// JSON serialization options configured for pretty printing with camelCase property naming.
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of StateWriter with the specified state file path. Creates directory if needed and loads existing state from disk.
        /// </summary>
        public StateWriter(string stateFilePath)
        {
            if (string.IsNullOrWhiteSpace(stateFilePath))
                throw new ArgumentNullException(nameof(stateFilePath));

            _stateFilePath = stateFilePath;
            _stateEntries = new Dictionary<string, StateEntry>();

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            EnsureDirectoryExists();
            LoadExistingState();
        }

        /// <summary>
        /// Updates the real-time state of a backup job and immediately persists the changes to disk.
        /// </summary>
        public void UpdateJobState(BackupJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            lock (_lock)
            {
                var stateEntry = StateEntry.FromBackupJob(job);
                _stateEntries[job.Name] = stateEntry;

                WriteStateToDisk();
            }
        }

        /// <summary>
        /// Removes a job state entry by name and persists the change to disk.
        /// </summary>
        public bool RemoveJobState(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
                throw new ArgumentNullException(nameof(jobName));

            lock (_lock)
            {
                bool removed = _stateEntries.Remove(jobName);

                if (removed)
                {
                    WriteStateToDisk();
                }

                return removed;
            }
        }

        /// <summary>
        /// Returns a snapshot copy of the current state of all backup jobs to prevent external modifications.
        /// </summary>
        public Dictionary<string, StateEntry> GetCurrentState()
        {
            lock (_lock)
            {
                return new Dictionary<string, StateEntry>(_stateEntries);
            }
        }

        /// <summary>
        /// Retrieves the state entry for a specific backup job by name, or null if not found.
        /// </summary>
        public StateEntry? GetJobState(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
                throw new ArgumentNullException(nameof(jobName));

            lock (_lock)
            {
                return _stateEntries.TryGetValue(jobName, out var state) ? state : null;
            }
        }

        /// <summary>
        /// Clears all job state entries and persists the empty state to disk.
        /// </summary>
        public void ClearAllStates()
        {
            lock (_lock)
            {
                _stateEntries.Clear();
                WriteStateToDisk();
            }
        }

        /// <summary>
        /// Serializes current state to JSON and writes to disk with error handling to prevent backup interruption.
        /// </summary>
        private void WriteStateToDisk()
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(_stateEntries, _jsonOptions);

                File.WriteAllText(_stateFilePath, jsonContent);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"[StateWriter] Failed to write state: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"[StateWriter] Access denied to state file: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads existing state from disk if the state file exists, or creates an empty state file if it does not. Handles corrupted JSON gracefully.
        /// </summary>
        private void LoadExistingState()
        {
            if (!File.Exists(_stateFilePath))
            {
                WriteStateToDisk();
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(_stateFilePath);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return;
                }

                var loadedStates = JsonSerializer.Deserialize<Dictionary<string, StateEntry>>(
                    jsonContent,
                    _jsonOptions
                );

                if (loadedStates != null)
                {
                    foreach (var kvp in loadedStates)
                    {
                        _stateEntries[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[StateWriter] Corrupted state file, starting fresh: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"[StateWriter] Failed to read state file: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates the directory containing the state file if it does not already exist.
        /// </summary>
        private void EnsureDirectoryExists()
        {
            string? directory = Path.GetDirectoryName(_stateFilePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}