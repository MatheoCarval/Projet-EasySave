    using Models;
    using Models.Enums;
    using EasySave.Services;
    using Services.Writers;
    using Utilities;
    using System.Text.Json;
    using System.Text.Json.Serialization;


    namespace Services.Managers;

    public class BackupManager
    {
        private readonly List<BackupJob> _jobs;
        private readonly int _maxJobs;
        private readonly FileTransferService _fileTransferService;
        private readonly StateWriter _stateWriter;

        // private readonly ConfigurationManger _configurationManager;

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

            _jobs = new List<BackupJob>();
            _maxJobs = maxJobs;
            _fileTransferService = fileTransferService;
            _stateWriter = stateWriter;

            LoadJobs();
        }

        /// <summary>
        /// Creates a new backup job
        /// </summary>
        /// <param name="name">Name of the backup job</param>
        /// <param name="sourcesPaths">List of source directory paths</param>
        /// <param name="targetPath">Target directory path</param>
        /// <param name="backupType">Type of backup (COMPLETE or DIFFERENTIAL)</param>
        /// <returns>The created backup job</returns>
        /// <exception cref="InvalidOperationException">Thrown when the maximum number of jobs is reached</exception>
        /// <exception cref="ArgumentException">Thrown if name already exists</exception>
        public BackupJob CreateJob(string name, List<string> sourcesPaths, string targetPath, BackupType backupType)
        {
            if (sourcesPaths.Count >= _maxJobs)
            {
                throw new InvalidOperationException($"Maximum number of jobs ({_maxJobs}) has been reached.");
            }

            if (_jobs.Any(j => j.Name == name))
            {
                throw new ArgumentException($"A job with name '{name}' already exists.", nameof(name));
            }

            var job = new BackupJob(name, sourcesPaths, targetPath, backupType);
            SaveJob(job);
            _jobs.Add(job);
            return job;
        }

        /// <summary>
        /// Deletes a backup job by its identifier
        /// </summary>
        /// <param name="jobName">Identifier of the job to delete</param>
        /// <returns>True if the job was deleted, False if it does not exist</returns>
        /// <exception cref="ArgumentException">Thrown if jobName is null or empty</exception>
        public bool DeleteJob(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                throw new ArgumentException("Job name cannot be null or empty.", nameof(jobName));
            }

            var job = _jobs.FirstOrDefault(j => j.Name == jobName);
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
                    if (PathValidator.IsDirectory(sourcePath))
                    {
                        _fileTransferService.TransferDirectory(sourcePath, job.TargetPath, job);
                    }
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

        /// <summary>
        /// Saves a backup job to the jobs.json file
        /// </summary>
        /// <param name="job">The backup job to save</param>
        /// <exception cref="ArgumentNullException">Thrown if job is null</exception>
        /// <exception cref="IOException">Thrown if there's an error writing to the file</exception>
        // TODO: Use configManager to get the path instead of hardcoding
        public void SaveJob(BackupJob job)
        {
            ArgumentNullException.ThrowIfNull(job);

            try
            {
                string jobsFilePath = "./Datas/jobs.json";
                var jobs = LoadJobsFromFile(jobsFilePath);

                // Remove job if it already exists (update case)
                jobs.RemoveAll(j => j.Name == job.Name);
                jobs.Add(job);

                SaveJobsToFile(jobsFilePath, jobs);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error saving job '{job.Name}' to jobs.json.", ex);
            }
        }

        /// <summary>
        /// Loads all backup jobs from the jobs.json file
        /// </summary>
        /// <exception cref="IOException">Thrown if there's an error reading the file</exception>
        // TODO: Use configManager to get the path instead of hardcoding
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

        /// <summary>
        /// Modifies an existing backup job
        /// </summary>
        /// <param name="jobId">Name of the job to modify</param>
        /// <param name="newName">New name for the job (optional)</param>
        /// <param name="newSourcePaths">New source paths (optional)</param>
        /// <param name="newTargetPath">New target path (optional)</param>
        /// <param name="newBackupType">New backup type (optional)</param>
        /// <exception cref="ArgumentException">Thrown if job does not exist or if new name already exists</exception>
        // TODO: Use configManager to get the path instead of hardcoding
        public void ModifyJob(string jobId, string? newName = null, List<string>? newSourcePaths = null, string? newTargetPath = null, BackupType? newBackupType = null)
        {
            var job = GetJob(jobId);
            if (job == null)
            {
                throw new ArgumentException($"Job with ID '{jobId}' does not exist.", nameof(jobId));
            }

            // Check if new name already exists (if provided and different from current)
            if (!string.IsNullOrWhiteSpace(newName) && newName != jobId && _jobs.Any(j => j.Name == newName))
            {
                throw new ArgumentException($"A job with name '{newName}' already exists.", nameof(newName));
            }

            try
            {
                // Update job properties
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
        /// Loads jobs from a JSON file
        /// </summary>
        /// <param name="filePath">Path to the jobs.json file</param>
        /// <returns>List of backup jobs</returns>
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
        /// Saves jobs to a JSON file
        /// </summary>
        /// <param name="filePath">Path to the jobs.json file</param>
        /// <param name="jobs">List of backup jobs to save</param>
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
            File.WriteAllText(filePath, jsonContent);
        }
    }