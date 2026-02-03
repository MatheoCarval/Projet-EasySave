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
    /// Manages real-time state of all backup jobs
    /// Writes to a single state.json file that is constantly updated
    /// </summary>
    public class StateWriter
    {
        // ==================== FIELDS ====================
        
        private readonly string _stateFilePath;
        private readonly Dictionary<string, StateEntry> _stateEntries;
        private readonly object _lock = new object();
        private readonly JsonSerializerOptions _jsonOptions;
        
        // ==================== CONSTRUCTOR ====================
        
        /// <summary>
        /// Initialize StateWriter with specified file path
        /// </summary>
        /// <param name="stateFilePath">Path to state.json file</param>
        public StateWriter(string stateFilePath)
        {
            if (string.IsNullOrWhiteSpace(stateFilePath))
                throw new ArgumentNullException(nameof(stateFilePath));
            
            _stateFilePath = stateFilePath;
            _stateEntries = new Dictionary<string, StateEntry>();
            
            // Configure JSON options for pretty printing
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true, // Pretty print with indentation
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };
            
            // Ensure directory exists
            EnsureDirectoryExists();
            
            // Load existing state if file exists
            LoadExistingState();
        }
        
        // ==================== PUBLIC METHODS ====================
        
        /// <summary>
        /// Update the state of a backup job in real-time
        /// </summary>
        /// <param name="job">BackupJob to update state for</param>
        public void UpdateJobState(BackupJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));
            
            lock (_lock) // Thread-safe
            {
                // Create or update state entry
                var stateEntry = StateEntry.FromBackupJob(job);
                _stateEntries[job.Name] = stateEntry;
                
                // Write to disk immediately (real-time requirement)
                WriteStateToDisk();
            }
        }
        
        /// <summary>
        /// Remove a job from the state file
        /// </summary>
        /// <param name="jobName">Name of the job to remove</param>
        /// <returns>True if removed, false if not found</returns>
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
        /// Get current state of all jobs (snapshot)
        /// </summary>
        /// <returns>Dictionary of job states</returns>
        public Dictionary<string, StateEntry> GetCurrentState()
        {
            lock (_lock)
            {
                // Return a copy to avoid external modifications
                return new Dictionary<string, StateEntry>(_stateEntries);
            }
        }
        
        /// <summary>
        /// Get state of a specific job
        /// </summary>
        /// <param name="jobName">Name of the job</param>
        /// <returns>StateEntry if found, null otherwise</returns>
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
        /// Clear all job states
        /// </summary>
        public void ClearAllStates()
        {
            lock (_lock)
            {
                _stateEntries.Clear();
                WriteStateToDisk();
            }
        }
        
        // ==================== PRIVATE METHODS ====================
        
        /// <summary>
        /// Write current state to disk (real-time)
        /// </summary>
        private void WriteStateToDisk()
        {
            try
            {
                // Serialize to JSON with pretty printing
                string jsonContent = JsonSerializer.Serialize(_stateEntries, _jsonOptions);
                
                // Write to file (overwrite)
                File.WriteAllText(_stateFilePath, jsonContent);
            }
            catch (IOException ex)
            {
                // Log error but don't crash the backup
                Console.Error.WriteLine($"[StateWriter] Failed to write state: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"[StateWriter] Access denied to state file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load existing state from disk if file exists
        /// </summary>
        private void LoadExistingState()
        {
            if (!File.Exists(_stateFilePath))
            {
                // No existing state, start fresh
                WriteStateToDisk(); // Create empty state file
                return;
            }
            
            try
            {
                string jsonContent = File.ReadAllText(_stateFilePath);
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    // Empty file, start fresh
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
                // Start with empty state
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"[StateWriter] Failed to read state file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Ensure the directory for state file exists
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