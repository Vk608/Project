using System;

namespace FABBatchValidator.Models
{
    /// <summary>
    /// Represents a batch validation job with status tracking.
    /// Used to manage background asynchronous validation without external queues.
    /// </summary>
    public class BatchJob
    {
        /// <summary>Unique identifier for this batch job.</summary>
        public string JobId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Current status of the job: Pending, Running, Completed, Failed.</summary>
        public JobStatus Status { get; set; } = JobStatus.Pending;

        /// <summary>Timestamp when the job was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Timestamp when the job started processing.</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>Timestamp when the job completed (either success or failure).</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>Total number of records processed in this job.</summary>
        public int TotalRecords { get; set; } = 0;

        /// <summary>Number of records successfully validated.</summary>
        public int SuccessfulRecords { get; set; } = 0;

        /// <summary>Number of records that failed validation.</summary>
        public int FailedRecords { get; set; } = 0;

        /// <summary>Error message if the job failed.</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Path to the results JSON file (populated on success).</summary>
        public string ResultsFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Duration of the job in seconds. Null if still running.
        /// </summary>
        public double? DurationSeconds
        {
            get
            {
                if (!StartedAt.HasValue || !CompletedAt.HasValue)
                    return null;
                return (CompletedAt.Value - StartedAt.Value).TotalSeconds;
            }
        }

        /// <summary>
        /// Human-readable status for API responses.
        /// </summary>
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    JobStatus.Pending => "Job queued, waiting to start",
                    JobStatus.Running => "Validation in progress",
                    JobStatus.Completed => "Job completed successfully",
                    JobStatus.Failed => "Job failed",
                    _ => "Unknown status"
                };
            }
        }
    }

    /// <summary>
    /// Enumeration of possible job statuses.
    /// </summary>
    public enum JobStatus
    {
        Pending = 0,    // Job created, not yet started
        Running = 1,    // Validation is in progress
        Completed = 2,  // Validation finished successfully
        Failed = 3      // Validation encountered an error
    }
}
