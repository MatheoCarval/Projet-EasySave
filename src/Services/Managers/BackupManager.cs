using Models;
using Models.Enums;
using EasySave.Services;
using Services.Writers;
using Utilities;
using System.Text.Json;
using System.Runtime.ConstrainedExecution;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Services.Managers;

/// <summary>
/// Token used to pause and resume a running backup job between file copies.
/// Based on ManualResetEventSlim: initially set (running), Reset = paused, Set = running.
/// </summary>
public class PauseToken
{
    private readonly ManualResetEventSlim _event = new(true); // true = initially running
    private bool _isAutoPaused;

    public bool IsPaused => !_event.IsSet;
    public bool IsAutoPaused => _isAutoPaused;

    /// <summary>Manual pause (user-triggered)</summary>
    public void Pause() { _event.Reset(); _isAutoPaused = false; }

    /// <summary>Automatic pause (blocked app detected)</summary>
    public void AutoPause() { _event.Reset(); _isAutoPaused = true; }

    /// <summary>Resume (manual or auto)</summary>
    public void Resume() { _event.Set(); _isAutoPaused = false; }

    /// <summary>Blocks the calling thread until the token is resumed.</summary>
    public void WaitIfPaused() { _event.Wait(); }
}

/// <summary>
/// Event arguments raised when jobs are automatically paused due to a blocked application.
/// </summary>
public class JobPauseEventArgs : EventArgs
{
    public List<string> JobIds { get; set; } = new();
    public List<string> BlockedApps { get; set; } = new();
}

/// <summary>
/// Global byte-level throttle for concurrent file transfers across all parallel backup jobs.
/// Limits the total bytes in-flight at any moment. A file can always start if nothing else
/// is currently transferring, even if its size exceeds the configured limit.
/// </summary>
public class TransferThrottle
{
    private long _limitBytes; // 0 = unlimited
    private long _bytesInFlight;
    private readonly object _lock = new();

    public bool HasLimit => _limitBytes > 0;

    public void SetLimit(long bytes)
    {
        lock (_lock)
        {
            _limitBytes = Math.Max(0, bytes);
            Monitor.PulseAll(_lock);
        }
    }

    /// <summary>
    /// From a list of pending file sizes, finds the FIRST file that fits under the limit
    /// (or any file if nothing is currently in flight). Blocks until at least one file can start.
    /// Marks the chosen file's bytes as in-flight and returns its index.
    /// </summary>
    public int AcquireFirstFitting(IReadOnlyList<long> pendingSizes)
    {
        lock (_lock)
        {
            while (true)
            {
                if (_limitBytes <= 0) // no limit configured
                {
                    _bytesInFlight += pendingSizes[0];
                    return 0;
                }

                for (int i = 0; i < pendingSizes.Count; i++)
                {
                    // Allow any file when nothing is in flight (avoids deadlock on files > limit)
                    if (_bytesInFlight == 0 || _bytesInFlight + pendingSizes[i] <= _limitBytes)
                    {
                        _bytesInFlight += pendingSizes[i];
                        return i;
                    }
                }

                Monitor.Wait(_lock); // wait for a Release() to pulse
            }
        }
    }

    /// <summary>Simple acquire for single-file transfers (not from TransferDirectory).</summary>
    public void Acquire(long fileSize)
    {
        lock (_lock)
        {
            while (_limitBytes > 0 && _bytesInFlight > 0 && _bytesInFlight + fileSize > _limitBytes)
                Monitor.Wait(_lock);
            _bytesInFlight += fileSize;
        }
    }

    public void Release(long bytes)
    {
        lock (_lock)
        {
            _bytesInFlight = Math.Max(0, _bytesInFlight - bytes);
            Monitor.PulseAll(_lock);
        }
    }
}

/// <summary>
/// Event arguments for file transfer progress
/// </summary>
public class FileProgressEventArgs : EventArgs
{
    public string JobId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string CurrentFile { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int RemainingFiles { get; set; }
    public int FilesProcessed => TotalFiles - RemainingFiles;
    public long TotalSize { get; set; }
    public long RemainingSize { get; set; }
    public int ProgressPercentage { get; set; }
}

/// <summary>
/// Manages backup job creation, execution, modification, and persistence with support for concurrent job handling and state tracking.
/// </summary>
public class BackupManager
{
    /// <summary>
    /// In-memory collection of all configured backup jobs.
    /// </summary>
    private readonly List<BackupJob> _jobs;
    /// <summary>
    /// Service responsible for transferring files and directories between source and target locations.
    /// </summary>
    private readonly FileTransferService _fileTransferService;
    /// <summary>
    /// Service responsible for persisting and managing backup job state information.
    /// </summary>
    private readonly StateWriter _stateWriter;
    private readonly string _jobsFilePath;
    private List<string> _blockedApplications;
    private List<string> _priorityExtensions;

    /// <summary>Pause tokens keyed by jobId for running jobs.</summary>
    private readonly Dictionary<string, PauseToken> _pauseTokens = new();
    private readonly object _pauseTokensLock = new();

    /// <summary>Ensures concurrent job completions don't corrupt jobs.json via simultaneous read-modify-write.</summary>
    private readonly object _jobsFileLock = new();

    /// <summary>Shared throttle passed to FileTransferService to cap total bytes in-flight across all parallel jobs.</summary>
    private readonly TransferThrottle _throttle = new();

    /// <summary>Set to 1 when auto-pause is active (blocked app running), reset to 0 when cleared.</summary>
    private int _autoPauseFired;

    /// <summary>
    /// Event raised when a file transfer completes during backup execution
    /// </summary>
    public event EventHandler<FileProgressEventArgs>? FileTransferred;

    /// <summary>Raised when all running jobs are automatically paused because a blocked application started.</summary>
    public event EventHandler<JobPauseEventArgs>? JobAutoPaused;
    /// <summary>Raised when all auto-paused jobs resume because the blocked application stopped.</summary>
    public event EventHandler<List<string>>? JobAutoResumed;

    /// <summary>
    /// Initializes a new instance of BackupManager with required services and loads existing backup jobs from persistent storage.
    /// </summary>
    public BackupManager(FileTransferService fileTransferService, StateWriter stateWriter, IEnumerable<string>? blockedApplications = null, string? jobsFilePath = null, IEnumerable<string>? priorityExtensions = null)
    {
        ArgumentNullException.ThrowIfNull(fileTransferService);
        ArgumentNullException.ThrowIfNull(stateWriter);

        _jobs = new List<BackupJob>();
        _fileTransferService = fileTransferService;
        _stateWriter = stateWriter;
        _blockedApplications = NormalizeBlockedApplications(blockedApplications);
        _priorityExtensions = priorityExtensions?.ToList() ?? new List<string>();
        _fileTransferService.SetPriorityExtensions(_priorityExtensions);
        _fileTransferService.SetThrottle(_throttle);
        _jobsFilePath = string.IsNullOrWhiteSpace(jobsFilePath) ? GetDefaultJobsFilePath() : jobsFilePath;

        MigrateLegacyJobsFileIfNeeded();

        // Subscribe to file transfer progress
        _fileTransferService.FileTransferred += OnFileTransferredFromService;

        LoadJobs();
    }

    /// <summary>
    /// Handles file transfer events from FileTransferService and raises FileTransferred event
    /// </summary>
    private void OnFileTransferredFromService(object? sender, FileProgressEventArgs e)
    {
        FileTransferred?.Invoke(this, e);
    }

    /// <summary>
    /// Creates a new backup job with the specified configuration, validates against job count limits and name uniqueness, and persists it to storage.
    /// </summary>
    public BackupJob CreateJob(string name, List<string> sourcesPaths, string targetPath, BackupType backupType)
    {
        if (_jobs.Any(j => j.Name == name))
        {
            throw new ArgumentException($"A job with name '{name}' already exists.", nameof(name));
        }

        var job = new BackupJob(name, sourcesPaths, targetPath, backupType);
        _jobs.Add(job);
        SaveJob(job);
        return job;
    }

    /// <summary>
    /// Deletes a backup job by ID from memory and removes its associated data from persistent storage.
    /// </summary>
    public bool DeleteJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
        }

        var job = _jobs.FirstOrDefault(j => j.Id == jobId);
        if (job != null)
        {
            _jobs.Remove(job);
            DeleteJobFile(job.Id);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Retrieves a backup job by its unique identifier, or null if not found.
    /// </summary>
    public BackupJob? GetJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier cannot be null or empty.", nameof(jobId));
        }

        return _jobs.FirstOrDefault(j => j.Id == jobId);
    }

    /// <summary>
    /// Retrieves a backup job by its name, or null if not found.
    /// </summary>
    public BackupJob? GetJobByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Job name cannot be null or empty.", nameof(name));
        }

        return _jobs.FirstOrDefault(j => j.Name == name);
    }

    /// <summary>
    /// Returns a copy of all configured backup jobs to prevent external modifications to the internal collection.
    /// </summary>
    public List<BackupJob> GetAllJobs()
    {
        return new List<BackupJob>(_jobs);
    }

    public void ExecuteJob(string jobId)
    {
        EnsureNoBlockedApplicationsRunning();

        var job = GetJob(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID '{jobId}' does not exist.", nameof(jobId));
        }

        var pauseToken = new PauseToken();
        lock (_pauseTokensLock) { _pauseTokens[jobId] = pauseToken; }

        // Poll for blocked apps every 2s while the job is running
        var blockedCheckTimer = new System.Threading.Timer(
            _ => CheckBlockedAppsForJob(jobId, pauseToken), null, 2000, 2000);

        try
        {
            // Calculate totals in a single pass
            job.TotalFiles = 0;
            job.TotalSize = 0;
            job.BackupState = BackupState.ACTIVE;

            foreach (var sourcePath in job.SourcePath)
            {
                if (PathValidator.IsDirectory(sourcePath))
                {
                    foreach (var fi in new DirectoryInfo(sourcePath).EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        job.TotalFiles++;
                        job.TotalSize += fi.Length;
                    }
                }
                else if (File.Exists(sourcePath))
                {
                    job.TotalFiles += 1;
                    job.TotalSize += new FileInfo(sourcePath).Length;
                }
                else
                {
                    throw new DirectoryNotFoundException(
                        $"Source path '{sourcePath}' does not exist for job '{jobId}'.");
                }
            }

            job.RemainingFiles = job.TotalFiles;
            job.RemainingSize = job.TotalSize;
            _stateWriter.UpdateJobState(job);

            foreach (var sourcePath in job.SourcePath)
            {
                if (PathValidator.IsDirectory(sourcePath))
                {
                    _fileTransferService.TransferDirectory(sourcePath, job.TargetPath, job, pauseToken);
                }
                else if (File.Exists(sourcePath))
                {
                    string fileName = Path.GetFileName(sourcePath);
                    string targetFile = Path.Combine(job.TargetPath, fileName);
                    _fileTransferService.TransferFile(sourcePath, targetFile, job, pauseToken);
                }
            }

            job.MarkAsCompleted();
            job.ErrorReason = null;
            _stateWriter.UpdateJobState(job);
            _stateWriter.Flush();
            _fileTransferService.FlushLogger();
            SaveJob(job);
        }
        catch (Exception ex)
        {
            job.MarkAsError();
            job.ErrorReason = GetUserFriendlyError(ex);
            _stateWriter.UpdateJobState(job);
            _stateWriter.Flush();
            _fileTransferService.FlushLogger();
            SaveJob(job);
            throw new InvalidOperationException($"Error executing job '{jobId}'.", ex);
        }
        finally
        {
            blockedCheckTimer.Dispose();
            lock (_pauseTokensLock)
            {
                _pauseTokens.Remove(jobId);
                if (_pauseTokens.Count == 0)
                    System.Threading.Interlocked.Exchange(ref _autoPauseFired, 0);
            }
        }
    }

    /// <summary>
    /// Executes a backup job asynchronously on a background thread.
    /// </summary>
    public Task ExecuteJobAsync(string jobId)
    {
        return Task.Run(() => ExecuteJob(jobId));
    }

    /// <summary>
    /// Executes multiple backup jobs in parallel asynchronously.
    /// Checks blocked applications once before launching all jobs.
    /// </summary>
    public async Task ExecuteJobsInParallelAsync(IEnumerable<string> jobIds)
    {
        EnsureNoBlockedApplicationsRunning();

        var tasks = jobIds.Select(jobId => Task.Run(() => ExecuteJob(jobId))).ToList();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes all backup jobs sequentially, collecting exceptions and throwing an AggregateException if any jobs fail.
    /// </summary>
    public void ExecuteAll()
    {
        EnsureNoBlockedApplicationsRunning();
        var exceptions = new List<Exception>();

        foreach (var job in _jobs)
        {
            try
            {
                ExecuteJob(job.Id);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException("One or more jobs failed.", exceptions);
        }
    }

    /// <summary>
    /// Executes all backup jobs sequentially without exception aggregation, stopping on first failure.
    /// </summary>
    public void ExecuteSequence()
    {
        EnsureNoBlockedApplicationsRunning();
        foreach (var job in _jobs)
        {
            ExecuteJob(job.Id);
        }
    }

    /// <summary>
    /// Manually pauses a running job. The backup thread will block between files.
    /// </summary>
    public void PauseJob(string jobId)
    {
        PauseToken? token;
        lock (_pauseTokensLock) { _pauseTokens.TryGetValue(jobId, out token); }
        if (token != null)
        {
            token.Pause();
            var job = GetJob(jobId);
            if (job != null) { job.BackupState = BackupState.PAUSED; _stateWriter.UpdateJobState(job); }
        }
    }

    /// <summary>
    /// Resumes a paused job.
    /// </summary>
    public void ResumeJob(string jobId)
    {
        PauseToken? token;
        lock (_pauseTokensLock) { _pauseTokens.TryGetValue(jobId, out token); }
        if (token != null)
        {
            token.Resume();
            var job = GetJob(jobId);
            if (job != null) { job.BackupState = BackupState.ACTIVE; _stateWriter.UpdateJobState(job); }
        }
    }

    /// <summary>
    /// Returns true if the job is currently paused.
    /// </summary>
    public bool IsJobPaused(string jobId)
    {
        PauseToken? token;
        lock (_pauseTokensLock) { _pauseTokens.TryGetValue(jobId, out token); }
        return token?.IsPaused ?? false;
    }

    /// <summary>
    /// Called by the per-job timer to detect blocked apps mid-execution.
    /// When a blocked app is detected, stops ALL running jobs immediately.
    /// Uses an atomic flag so only the first timer that fires handles the stop.
    /// </summary>
    private void CheckBlockedAppsForJob(string jobId, PauseToken pauseToken)
    {
        try
        {
            var running = GetRunningBlockedApps();

            if (running.Count > 0)
            {
                // Atomically claim the right to trigger the auto-pause (only one timer wins)
                if (System.Threading.Interlocked.CompareExchange(ref _autoPauseFired, 1, 0) != 0) return;

                List<(string Id, PauseToken Token)> snapshot;
                lock (_pauseTokensLock)
                {
                    snapshot = _pauseTokens.Select(kvp => (kvp.Key, kvp.Value)).ToList();
                }

                var pausedIds = new List<string>();
                foreach (var (id, token) in snapshot)
                {
                    if (!token.IsPaused)
                    {
                        token.AutoPause();
                        var job = GetJob(id);
                        if (job != null) { job.BackupState = BackupState.PAUSED; _stateWriter.UpdateJobState(job); }
                        pausedIds.Add(id);
                    }
                }

                if (pausedIds.Count > 0)
                {
                    JobAutoPaused?.Invoke(this, new JobPauseEventArgs
                    {
                        JobIds = pausedIds,
                        BlockedApps = running
                    });
                }
            }
            else if (System.Threading.Interlocked.CompareExchange(ref _autoPauseFired, 0, 1) == 1)
            {
                // Blocked app closed — resume ALL auto-paused jobs
                List<(string Id, PauseToken Token)> snapshot;
                lock (_pauseTokensLock)
                {
                    snapshot = _pauseTokens.Select(kvp => (kvp.Key, kvp.Value)).ToList();
                }

                var resumedIds = new List<string>();
                foreach (var (id, token) in snapshot)
                {
                    if (token.IsAutoPaused)
                    {
                        token.Resume();
                        var job = GetJob(id);
                        if (job != null) { job.BackupState = BackupState.ACTIVE; _stateWriter.UpdateJobState(job); }
                        resumedIds.Add(id);
                    }
                }

                if (resumedIds.Count > 0)
                {
                    JobAutoResumed?.Invoke(this, resumedIds);
                }
            }
        }
        catch
        {
            // Never crash the timer callback
        }
    }

    /// <summary>
    /// Returns the list of currently-running processes that are in the blocked-applications list.
    /// </summary>
    private List<string> GetRunningBlockedApps()
    {
        if (_blockedApplications.Count == 0)
            return new List<string>();

        var runningProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var name = process.ProcessName;
                if (!string.IsNullOrWhiteSpace(name))
                    runningProcesses.Add(name);
            }
            catch { }
        }

        return _blockedApplications
            .Where(app => runningProcesses.Contains(app))
            .ToList();
    }

    private void EnsureNoBlockedApplicationsRunning()
    {
        var blockedRunning = GetRunningBlockedApps();
        if (blockedRunning.Count > 0)
        {
            throw new InvalidOperationException(
                $"Backup blocked because these applications are running: {string.Join(", ", blockedRunning)}");
        }
    }

    private static List<string> NormalizeBlockedApplications(IEnumerable<string>? blockedApplications)
    {
        if (blockedApplications == null)
        {
            return new List<string>();
        }

        return blockedApplications
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Select(app => NormalizeProcessName(app))
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Updates the list of blocked applications at runtime (e.g. after settings change).
    /// </summary>
    public void UpdateBlockedApplications(IEnumerable<string>? blockedApplications)
    {
        _blockedApplications = NormalizeBlockedApplications(blockedApplications);
    }

    /// <summary>
    /// Updates the ordered priority extensions list at runtime (e.g. after settings change).
    /// </summary>
    public void UpdatePriorityExtensions(IEnumerable<string>? priorityExtensions)
    {
        _priorityExtensions = priorityExtensions?.ToList() ?? new List<string>();
        _fileTransferService.SetPriorityExtensions(_priorityExtensions);
    }

    /// <summary>
    /// Updates the maximum parallel transfer size limit. 0 = unlimited.
    /// The throttle is shared across all running jobs via FileTransferService.
    /// </summary>
    public void UpdateMaxParallelSize(long value, string unit)
    {
        if (value <= 0) { _throttle.SetLimit(0); return; }
        long bytes = unit switch
        {
            "KB" => value * 1024L,
            "MB" => value * 1024L * 1024L,
            "TB" => value * 1024L * 1024L * 1024L * 1024L,
            _    => value * 1024L * 1024L * 1024L   // "GB" default
        };
        _throttle.SetLimit(bytes);
    }

    /// <summary>
    /// Updates the logger instance used for recording backup operations.
    /// This allows changing the log format (JSON/XML) without restarting the application.
    /// </summary>
    public void UpdateLogger(EasyLog.Abstractions.ILogger logger)
    {
        _fileTransferService.UpdateLogger(logger);
    }

    private static string NormalizeProcessName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^4];
        }

        return trimmed;
    }

    /// <summary>
    /// Persists a single backup job to storage, either updating an existing job or creating a new entry, then reloads all jobs.
    /// </summary>
    public void SaveJob(BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        lock (_jobsFileLock)
        {
            try
            {
                var jobs = LoadJobsFromFile(_jobsFilePath);

                // Find and remove existing job (search by ID)
                var existingIndex = jobs.FindIndex(j => j.Id == job.Id);
                if (existingIndex >= 0)
                {
                    jobs[existingIndex] = job; // Replace instead of remove/add
                }
                else
                {
                    jobs.Add(job); // New job
                }

                SaveJobsToFile(_jobsFilePath, jobs);

                // Reload jobs to sync _jobs list
                _jobs.Clear();
                _jobs.AddRange(jobs);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error saving job '{job.Id}' to jobs.json.", ex);
            }
        }
    }

    /// <summary>
    /// Loads all backup jobs from persistent storage and populates the internal job collection.
    /// </summary>
    public void LoadJobs()
    {
        try
        {
            var jobs = LoadJobsFromFile(_jobsFilePath);
            _jobs.Clear();
            _jobs.AddRange(jobs);
        }
        catch (Exception ex)
        {
            throw new IOException("Error loading jobs from jobs.json.", ex);
        }
    }

    /// <summary>
    /// Modifies an existing backup job with new configuration values and persists the changes to storage.
    /// </summary>
    public void ModifyJob(string jobId, string? newName = null, List<string>? newSourcePaths = null, string? newTargetPath = null, BackupType? newBackupType = null)
    {
        var job = GetJob(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID '{jobId}' does not exist.", nameof(jobId));
        }

        // Check for duplicate name (exclude current job)
        if (!string.IsNullOrWhiteSpace(newName) && newName != job.Name && _jobs.Any(j => j.Id != jobId && j.Name == newName))
        {
            throw new ArgumentException($"A job with name '{newName}' already exists.", nameof(newName));
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(newName))
                job.Name = newName;
            if (newSourcePaths != null)
                job.SourcePath = newSourcePaths;
            if (!string.IsNullOrWhiteSpace(newTargetPath))
                job.TargetPath = newTargetPath;
            if (newBackupType.HasValue)
                job.BackupType = newBackupType.Value;

            SaveJob(job);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error modifying job '{jobId}'.", ex);
        }
    }

    /// <summary>
    /// Loads and deserializes backup jobs from a JSON file, returning an empty list if the file does not exist or is empty.
    /// </summary>
    private List<BackupJob> LoadJobsFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<BackupJob>();
        }

        string jsonContent = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new List<BackupJob>();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        try
        {
            return JsonSerializer.Deserialize<List<BackupJob>>(jsonContent, options) ?? new List<BackupJob>();
        }
        catch (JsonException)
        {
            try
            {
                var singleJob = JsonSerializer.Deserialize<BackupJob>(jsonContent, options);
                if (singleJob != null &&
                    !string.IsNullOrWhiteSpace(singleJob.Name) &&
                    !string.IsNullOrWhiteSpace(singleJob.TargetPath) &&
                    singleJob.SourcePath != null &&
                    singleJob.SourcePath.Count > 0)
                {
                    return new List<BackupJob> { singleJob };
                }
            }
            catch (JsonException)
            {
                // Ignore and fallback to empty list.
            }

            return new List<BackupJob>();
        }
    }

    /// <summary>
    /// Serializes backup jobs to JSON format and writes them to a file using atomic write operations (temp file then move) for data integrity.
    /// </summary>
    private void SaveJobsToFile(string filePath, List<BackupJob> jobs)
    {
        string directory = Path.GetDirectoryName(filePath) ?? GetDefaultJobsDirectory();
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        string jsonContent = JsonSerializer.Serialize(jobs, options);

        string tempFile = filePath + ".tmp";
        File.WriteAllText(tempFile, jsonContent);

        if (File.Exists(filePath))
            File.Delete(filePath);
        File.Move(tempFile, filePath);
    }

    /// <summary>
    /// Removes a backup job from persistent storage by its ID and saves the updated job collection.
    /// </summary>
    private void DeleteJobFile(string jobId)
    {
        try
        {
            var jobs = LoadJobsFromFile(_jobsFilePath);

            jobs.RemoveAll(j => j.Id == jobId);

            SaveJobsToFile(_jobsFilePath, jobs);
        }
        catch (Exception ex)
        {
            throw new IOException($"Error deleting job '{jobId}' from jobs.json.", ex);
        }
    }

    private static string GetDefaultJobsFilePath()
    {
        return Path.Combine(GetDefaultJobsDirectory(), "jobs.json");
    }

    private static string GetDefaultJobsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave");
    }

    private void MigrateLegacyJobsFileIfNeeded()
    {
        if (!string.Equals(_jobsFilePath, GetDefaultJobsFilePath(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string legacyPath = Path.Combine(AppContext.BaseDirectory, "Datas", "jobs.json");
        if (!File.Exists(legacyPath) || File.Exists(_jobsFilePath))
        {
            return;
        }

        string? targetDirectory = Path.GetDirectoryName(_jobsFilePath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        File.Copy(legacyPath, _jobsFilePath);
    }

    /// <summary>
    /// Maps an exception to a short error key for display on the job card.
    /// </summary>
    private static string GetUserFriendlyError(Exception ex)
    {
        var inner = ex is InvalidOperationException ? (ex.InnerException ?? ex) : ex;

        if (inner is DirectoryNotFoundException)
            return "error_path_not_found";
        if (inner is UnauthorizedAccessException)
            return "error_access_denied";
        if (inner is IOException)
            return "error_io";

        return "error_generic";
    }
}