using Models;
using Models.Enums;
using EasySave.Services;
using Services.Writers;
using Utilities;
using System.Text.Json;

namespace Services.Managers;

public class BackupManager
{
    private readonly List<BackupJob> _jobs;
    private readonly int _maxJobs;
    private readonly FileTransferService _fileTransferService;
    private readonly StateWriter _stateWriter;

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

    public bool DeleteJob(string jobName)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            throw new ArgumentException("Job name cannot be null or empty.", nameof(jobName));
        }

        var job = _jobs.FirstOrDefault(j => j.Name == jobName);
        if (job != null)
        {
            _jobs.Remove(job);
            DeleteJobFile(job.Id);
            return true;
        }
        return false;
    }

    public BackupJob? GetJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier cannot be null or empty.", nameof(jobId));
        }

        return _jobs.FirstOrDefault(j => j.Id == jobId);
    }

    public BackupJob? GetJobByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Job name cannot be null or empty.", nameof(name));
        }

        return _jobs.FirstOrDefault(j => j.Name == name);
    }

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


            // Now transfer all sources
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

    public void ExecuteSequence()
    {
        foreach (var job in _jobs)
        {
            ExecuteJob(job.Id);
        }
    }

    public void SaveJob(BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        try
        {
            string path = "./Datas/jobs.json";
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

    public void LoadJobs()
    {
        try
        {
            string jobsFilePath = "./Datas/jobs.json";
            var jobs = LoadJobsFromFile(jobsFilePath);
            _jobs.Clear();
            _jobs.AddRange(jobs);
        }
        catch (Exception ex)
        {
            throw new IOException("Error loading jobs from jobs.json.", ex);
        }
    }

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

    private void DeleteJobFile(string jobId)
    {
        try
        {
            string jobsFilePath = "./Datas/jobs.json";
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