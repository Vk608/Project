using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FABBatchValidator.Models;

namespace FABBatchValidator.Services
{
    /// <summary>
    /// Manages background batch validation jobs.
    /// Tracks job state, provides job lookup, and persists job metadata to disk.
    /// No external queues or schedulers - uses the thread pool via Task.Run.
    /// </summary>
    public class BackgroundJobManager
    {
        private readonly ConcurrentDictionary<string, BatchJob> _jobs;
        private readonly string _jobMetadataPath;
        private readonly JsonSerializerOptions _jsonOptions;
        private static readonly SemaphoreSlim _persistenceLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Latest completed job (tracks the most recent successful or failed job).
        /// </summary>
        private BatchJob _latestJob;

        public BackgroundJobManager(string jobMetadataPath = "jobs-metadata.json")
        {
            _jobs = new ConcurrentDictionary<string, BatchJob>();
            _jobMetadataPath = jobMetadataPath;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Load existing jobs from disk on startup
            LoadJobsFromDisk();
        }

        /// <summary>
        /// Create a new batch job and return it.
        /// The job starts in Pending state.
        /// </summary>
        public BatchJob CreateJob()
        {
            var job = new BatchJob
            {
                JobId = Guid.NewGuid().ToString(),
                Status = JobStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _jobs.TryAdd(job.JobId, job);
            _ = PersistJobsToDiskAsync(CancellationToken.None); // Fire and forget

            return job;
        }

        /// <summary>
        /// Get a specific job by ID.
        /// Returns null if job not found.
        /// </summary>
        public BatchJob GetJob(string jobId)
        {
            _jobs.TryGetValue(jobId, out var job);
            return job;
        }

        /// <summary>
        /// Get all jobs, optionally filtered by status.
        /// </summary>
        public List<BatchJob> GetAllJobs(JobStatus? statusFilter = null)
        {
            if (statusFilter == null)
                return _jobs.Values.OrderByDescending(j => j.CreatedAt).ToList();

            return _jobs.Values
                .Where(j => j.Status == statusFilter)
                .OrderByDescending(j => j.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Get the latest completed job (most recent regardless of success/failure).
        /// </summary>
        public BatchJob GetLatestJob()
        {
            return _latestJob;
        }

        /// <summary>
        /// Mark a job as running.
        /// </summary>
        public void MarkJobAsRunning(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.Status = JobStatus.Running;
                job.StartedAt = DateTime.UtcNow;
                _ = PersistJobsToDiskAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Mark a job as completed successfully.
        /// </summary>
        public void MarkJobAsCompleted(string jobId, int totalRecords, int successfulRecords, 
            int failedRecords, string resultsFilePath)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.TotalRecords = totalRecords;
                job.SuccessfulRecords = successfulRecords;
                job.FailedRecords = failedRecords;
                job.ResultsFilePath = resultsFilePath;
                _latestJob = job;
                _ = PersistJobsToDiskAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Mark a job as failed.
        /// </summary>
        public void MarkJobAsFailed(string jobId, string errorMessage, int totalRecords = 0, 
            int successfulRecords = 0, int failedRecords = 0)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.ErrorMessage = errorMessage;
                job.TotalRecords = totalRecords;
                job.SuccessfulRecords = successfulRecords;
                job.FailedRecords = failedRecords;
                _latestJob = job;
                _ = PersistJobsToDiskAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Persist all jobs to disk (JSON metadata file).
        /// Uses semaphore to prevent concurrent writes.
        /// </summary>
        private async Task PersistJobsToDiskAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _persistenceLock.WaitAsync(cancellationToken);

                var jobList = _jobs.Values.ToList();
                var json = JsonSerializer.Serialize(jobList, _jsonOptions);
                var directory = Path.GetDirectoryName(_jobMetadataPath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(_jobMetadataPath, json, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BackgroundJobManager] Warning: Failed to persist jobs: {ex.Message}");
            }
            finally
            {
                _persistenceLock.Release();
            }
        }

        /// <summary>
        /// Load jobs from disk on startup.
        /// </summary>
        private void LoadJobsFromDisk()
        {
            try
            {
                if (!File.Exists(_jobMetadataPath))
                    return;

                var json = File.ReadAllText(_jobMetadataPath);
                var jobs = JsonSerializer.Deserialize<List<BatchJob>>(json, _jsonOptions);

                if (jobs != null)
                {
                    foreach (var job in jobs)
                    {
                        _jobs.TryAdd(job.JobId, job);
                        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed)
                        {
                            if (_latestJob == null || job.CompletedAt > _latestJob.CompletedAt)
                            {
                                _latestJob = job;
                            }
                        }
                    }

                    Console.WriteLine($"[BackgroundJobManager] Loaded {jobs.Count} jobs from disk.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BackgroundJobManager] Warning: Failed to load jobs from disk: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear old jobs (older than specified days). Useful for cleanup.
        /// </summary>
        public int CleanupOldJobs(int daysOld = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var oldJobIds = _jobs
                .Where(kvp => kvp.Value.CompletedAt.HasValue && kvp.Value.CompletedAt < cutoffDate)
                .Select(kvp => kvp.Key)
                .ToList();

            int removed = 0;
            foreach (var jobId in oldJobIds)
            {
                if (_jobs.TryRemove(jobId, out _))
                    removed++;
            }

            if (removed > 0)
            {
                _ = PersistJobsToDiskAsync(CancellationToken.None);
            }

            return removed;
        }
    }
}
