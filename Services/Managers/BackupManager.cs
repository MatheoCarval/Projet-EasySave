using Models;
using Models.Enums;
using EasySave.Services;
using Services.Writers;
using Utilities;
using System.Text.Json;
using System.Runtime.ConstrainedExecution;

namespace Services.Managers;

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
    /// Maximum number of backup jobs allowed in the system.
    /// </summary>
    private readonly int _maxJobs;
    /// <summary>
    /// Service responsible for transferring files and directories between source and target locations.
    /// </summary>
    private readonly FileTransferService _fileTransferService;
    /// <summary>
    /// Service responsible for persisting and managing backup job state information.
    /// </summary>
    private readonly StateWriter _stateWriter;

    const string JobsFilePath = "./Datas/jobs.json";

    /// <summary>
    /// Initializes a new instance of BackupManager with required services and loads existing backup jobs from persistent storage.
    /// </summary>
    public BackupManager(FileTransferService fileTransferService, StateWriter stateWriter, int maxJobs = 5)
    {
        ArgumentNullException.ThrowIfNull(fileTransferService);
        ArgumentNullException.ThrowIfNull(stateWriter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxJobs);

        _jobs = new List<BackupJob>();
        _maxJobs = maxJobs;
        _fileTransferService = fileTransferService;
        _stateWriter = stateWriter;

        LoadJobs();
    }

    /// <summary>
    /// Creates a new backup job with the specified configuration, validates against job count limits and name uniqueness, and persists it to storage.
    /// </summary>
    public BackupJob CreateJob(string name, List<string> sourcesPaths, string targetPath, BackupType backupType)
    {
        if (_jobs.Count >= _maxJobs)
        {
            throw new InvalidOperationException($"Maximum number of jobs ({_maxJobs}) has been reached.");
        }

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
        var job = GetJob(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID '{jobId}' does not exist.", nameof(jobId));
        }

        try
        {
            // Calculate totals BEFORE starting transfers
            job.TotalFiles = 0;
            job.TotalSize = 0;
            job.BackupState = BackupState.ACTIVE;

            foreach (var sourcePath in job.SourcePath)
            {
                if (PathValidator.IsDirectory(sourcePath))
                {
                    var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                    job.TotalFiles += files.Length;
                    job.TotalSize += files.Sum(f => new FileInfo(f).Length);
                }
                else if (File.Exists(sourcePath))
                {
                    job.TotalFiles += 1;
                    job.TotalSize += new FileInfo(sourcePath).Length;
                }
                else
                {
                    // Fail fast on misconfigured jobs: a configured source path does not exist.
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
                    _fileTransferService.TransferDirectory(sourcePath, job.TargetPath, job);
                }
                else if (File.Exists(sourcePath))
                {
                    string fileName = Path.GetFileName(sourcePath);
                    string targetFile = Path.Combine(job.TargetPath, fileName);
                    _fileTransferService.TransferFile(sourcePath, targetFile, job);
                }
            }

            job.MarkAsCompleted();
            _stateWriter.UpdateJobState(job);
        }
        catch (Exception ex)
        {
            job.MarkAsError();
            _stateWriter.UpdateJobState(job);
            throw new InvalidOperationException($"Error executing job '{jobId}'.", ex);
        }
    }

    /// <summary>
    /// Executes all backup jobs sequentially, collecting exceptions and throwing an AggregateException if any jobs fail.
    /// </summary>
    public void ExecuteAll()
    {
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
        foreach (var job in _jobs)
        {
            ExecuteJob(job.Id);
        }
    }

    /// <summary>
    /// Persists a single backup job to storage, either updating an existing job or creating a new entry, then reloads all jobs.
    /// </summary>
    public void SaveJob(BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        try
        {
            string path = JobsFilePath;
            var jobs = LoadJobsFromFile(path);

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

            SaveJobsToFile(path, jobs);

            // Reload jobs to sync _jobs list
            LoadJobs();
        }
        catch (Exception ex)
        {
            throw new IOException($"Error saving job '{job.Id}' to jobs.json.", ex);
        }
    }

    /// <summary>
    /// Loads all backup jobs from persistent storage and populates the internal job collection.
    /// </summary>
    public void LoadJobs()
    {
        try
        {
            string jobsFilePath = JobsFilePath;
            var jobs = LoadJobsFromFile(jobsFilePath);
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

        return JsonSerializer.Deserialize<List<BackupJob>>(jsonContent, options) ?? new List<BackupJob>();
    }

    /// <summary>
    /// Serializes backup jobs to JSON format and writes them to a file using atomic write operations (temp file then move) for data integrity.
    /// </summary>
    private void SaveJobsToFile(string filePath, List<BackupJob> jobs)
    {
        string directory = Path.GetDirectoryName(filePath) ?? "./Datas";
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
            string jobsFilePath = JobsFilePath;
            var jobs = LoadJobsFromFile(jobsFilePath);

            jobs.RemoveAll(j => j.Id == jobId);

            SaveJobsToFile(jobsFilePath, jobs);
        }
        catch (Exception ex)
        {
            throw new IOException($"Error deleting job '{jobId}' from jobs.json.", ex);
        }
    }
}