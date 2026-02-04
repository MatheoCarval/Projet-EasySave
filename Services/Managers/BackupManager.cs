using Models;
using Models.Enums;
using EasySave.Services;
using Services.Writers;

namespace EasySave.Services.Managers;

public class BackupManager
{
    private readonly List<BackupJob> _jobs;
    private readonly int _maxJobs;
    private readonly FileTransferService _fileTransferService;
    private readonly StateWriter _stateWriter;

    /// <summary>
    /// Initializes a new instance of the backup manager
    /// </summary>
    /// <param name="fileTransferService">File transfer service</param>
    /// <param name="stateWriter">State writing service</param>
    /// <param name="maxJobs">Maximum number of jobs allowed (default: 5)</param>
    /// <exception cref="ArgumentNullException">Thrown if fileTransferService or stateWriter is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxJobs is less than or equal to 0</exception>
    public BackupManager(FileTransferService fileTransferService, StateWriter stateWriter, int maxJobs = 5)
    {
        ArgumentNullException.ThrowIfNull(fileTransferService);
        ArgumentNullException.ThrowIfNull(stateWriter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxJobs);

        _jobs = new List<BackupJob>(maxJobs);
        _maxJobs = maxJobs;
        _fileTransferService = fileTransferService;
        _stateWriter = stateWriter;
    }

    /// <summary>
    /// Creates a new backup job
    /// </summary>
    /// <param name="name">Name of the backup job</param>
    /// <param name="sourcePath">Source directory path</param>
    /// <param name="targetPath">Target directory path</param>
    /// <param name="backupType">Type of backup (COMPLETE or DIFFERENTIAL)</param>
    /// <returns>The created backup job</returns>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of jobs is reached</exception>
    /// <exception cref="ArgumentException">Thrown if name already exists</exception>
    public BackupJob CreateJob(string name, string sourcePath, string targetPath, BackupType backupType)
    {
        if (_jobs.Count >= _maxJobs)
        {
            throw new InvalidOperationException($"Maximum number of jobs ({_maxJobs}) has been reached.");
        }

        if (_jobs.Any(j => j.Name == name))
        {
            throw new ArgumentException($"A job with name '{name}' already exists.", nameof(name));
        }

        var job = new BackupJob(name, sourcePath, targetPath, backupType);
        _jobs.Add(job);
        return job;
    }

    /// <summary>
    /// Deletes a backup job by its identifier
    /// </summary>
    /// <param name="jobId">Identifier of the job to delete</param>
    /// <returns>True if the job was deleted, False if it does not exist</returns>
    /// <exception cref="ArgumentException">Thrown if jobId is null or empty</exception>
    public bool DeleteJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier cannot be null or empty.", nameof(jobId));
        }

        var job = _jobs.FirstOrDefault(j => j.Name == jobId);
        return job != null && _jobs.Remove(job);
    }

    /// <summary>
    /// Gets a backup job by its identifier
    /// </summary>
    /// <param name="jobId">Identifier of the job to retrieve</param>
    /// <returns>The corresponding backup job, or null if it does not exist</returns>
    /// <exception cref="ArgumentException">Thrown if jobId is null or empty</exception>
    public BackupJob? GetJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier cannot be null or empty.", nameof(jobId));
        }

        return _jobs.FirstOrDefault(j => j.Name == jobId);
    }

    /// <summary>
    /// Gets all backup jobs
    /// </summary>
    /// <returns>A copy of the list of all backup jobs</returns>
    public List<BackupJob> GetAllJobs()
    {
        return new List<BackupJob>(_jobs);
    }

    /// <summary>
    /// Executes a specific backup job
    /// </summary>
    /// <param name="jobId">Identifier of the job to execute</param>
    /// <exception cref="ArgumentException">Thrown when the job does not exist or if jobId is null or empty</exception>
    public void ExecuteJob(string jobId)
    {
        var job = GetJob(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID '{jobId}' does not exist.", nameof(jobId));
        }

        try
        {
            // Transfer all source paths
            foreach (var sourcePath in job.SourcePath)
            {
                _fileTransferService.TransferDirectory(sourcePath, job.TargetPath, job);
            }
        }
        catch (Exception ex)
        {
            job.MarkAsError();
            _stateWriter.UpdateJobState(job);
            throw new InvalidOperationException($"Error executing job '{jobId}'.", ex);
        }
    }

    /// <summary>
    /// Executes all backup jobs
    /// </summary>
    /// <exception cref="AggregateException">Thrown if one or more jobs fail</exception>
    public void ExecuteAll()
    {
        var exceptions = new List<Exception>();

        foreach (var job in _jobs)
        {
            try
            {
                ExecuteJob(job.Name);
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
    /// Executes all backup jobs sequentially, stops at the first error
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if a job fails</exception>
    public void ExecuteSequence()
    {
        foreach (var job in _jobs)
        {
            ExecuteJob(job.Name);
        }
    }
}