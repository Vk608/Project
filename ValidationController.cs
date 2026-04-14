using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FABBatchValidator.Configuration;
using FABBatchValidator.Excel;
using FABBatchValidator.Models;
using FABBatchValidator.Services;
using FABBatchValidator.Storage;

namespace FABBatchValidator.Controllers
{
    /// <summary>
    /// API endpoint for triggering batch validation of bibliographic records.
    /// NOW: Uses background/asynchronous processing - POST returns immediately with a job ID.
    /// GET: Provides job status tracking.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ValidationController : ControllerBase
    {
        private readonly PipelineConfiguration _config;
        private readonly ValidationService _validationService;
        private readonly JsonResultRepository _resultRepository;
        private readonly ExcelInputReader _inputReader;
        private readonly BackgroundJobManager _jobManager;

        public ValidationController(
            PipelineConfiguration config,
            ValidationService validationService,
            JsonResultRepository resultRepository,
            ExcelInputReader inputReader,
            BackgroundJobManager jobManager)
        {
            _config = config;
            _validationService = validationService;
            _resultRepository = resultRepository;
            _inputReader = inputReader;
            _jobManager = jobManager;
        }

        /// <summary>
        /// POST /api/validation/validate
        /// ASYNC: Triggers the batch validation pipeline in the background.
        /// - Returns immediately with job ID and status
        /// - Validation runs asynchronously (does NOT block HTTP request)
        /// - Results are stored in JSON when complete
        /// - Use GET /api/validation/job/{jobId} to check status
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> Validate(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No input file provided." });
                }

                // Create a new background job (starts in Pending state)
                var job = _jobManager.CreateJob();
                Console.WriteLine($"[ValidationController] Created job {job.JobId}");

                // Ensure Storage/Inputs directory exists
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Inputs");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Clean the file name and append the jobId to ensure uniqueness
                var safeFileName = Path.GetFileName(file.FileName);
                var inputFilePath = Path.Combine(uploadsDir, $"{job.JobId}_{safeFileName}");

                // Save uploaded file locally
                using (var stream = new FileStream(inputFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Read records briefly just to get the count to return immediately in HTTP response
                // Then actual validation happens in background
                var records = _inputReader.ReadRecords(inputFilePath);
                
                if (records.Count == 0)
                {
                    // Cleanup empty invalid file
                    System.IO.File.Delete(inputFilePath);
                    return BadRequest(new { error = "No records found in input file" });
                }

                Console.WriteLine($"[ValidationController] Parsed {records.Count} records. Starting Async worker...");

                // Start validation asynchronously (fire-and-forget on thread pool)
                var worker = new AsyncValidationWorker(_validationService, _resultRepository, _inputReader, _jobManager);
                _ = Task.Run(async () => await worker.ExecuteValidationAsync(job.JobId, inputFilePath));

                // Return immediately with job info
                return Accepted(new
                {
                    jobId = job.JobId,
                    status = job.Status.ToString(),
                    statusDisplay = job.StatusDisplay,
                    createdAt = job.CreatedAt,
                    message = "Validation job queued. Check status with GET /api/validation/job/{jobId}",
                    inputRecordCount = records.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to queue validation job",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/validation/job/{jobId}
        /// Retrieve the current status of a specific validation job.
        /// </summary>
        [HttpGet("job/{jobId}")]
        public IActionResult GetJobStatus(string jobId)
        {
            try
            {
                var job = _jobManager.GetJob(jobId);
                
                if (job == null)
                {
                    return NotFound(new { error = "Job not found", jobId = jobId });
                }

                return Ok(new
                {
                    jobId = job.JobId,
                    status = job.Status.ToString(),
                    statusDisplay = job.StatusDisplay,
                    createdAt = job.CreatedAt,
                    startedAt = job.StartedAt,
                    completedAt = job.CompletedAt,
                    durationSeconds = job.DurationSeconds,
                    totalRecords = job.TotalRecords,
                    successfulRecords = job.SuccessfulRecords,
                    failedRecords = job.FailedRecords,
                    errorMessage = job.ErrorMessage,
                    resultsFile = job.ResultsFilePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to retrieve job status",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/validation/jobs
        /// Retrieve all validation jobs (latest first).
        /// Optional query parameter: ?status=Running|Completed|Failed|Pending
        /// </summary>
        [HttpGet("jobs")]
        public IActionResult GetAllJobs([FromQuery] string? status = null)
        {
            try
            {
                JobStatus? statusFilter = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<JobStatus>(status, true, out var parsedStatus))
                {
                    statusFilter = parsedStatus;
                }

                var jobs = _jobManager.GetAllJobs(statusFilter);

                return Ok(new
                {
                    count = jobs.Count,
                    jobs = jobs.ConvertAll(j => new
                    {
                        jobId = j.JobId,
                        status = j.Status.ToString(),
                        statusDisplay = j.StatusDisplay,
                        createdAt = j.CreatedAt,
                        completedAt = j.CompletedAt,
                        durationSeconds = j.DurationSeconds,
                        successfulRecords = j.SuccessfulRecords,
                        failedRecords = j.FailedRecords
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to retrieve jobs",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/validation/latest
        /// Retrieve the latest completed validation job (most recent regardless of success/failure).
        /// </summary>
        [HttpGet("latest")]
        public IActionResult GetLatestJob()
        {
            try
            {
                var job = _jobManager.GetLatestJob();
                
                if (job == null)
                {
                    return NotFound(new { message = "No completed jobs found" });
                }

                return Ok(new
                {
                    jobId = job.JobId,
                    status = job.Status.ToString(),
                    statusDisplay = job.StatusDisplay,
                    createdAt = job.CreatedAt,
                    completedAt = job.CompletedAt,
                    durationSeconds = job.DurationSeconds,
                    totalRecords = job.TotalRecords,
                    successfulRecords = job.SuccessfulRecords,
                    failedRecords = job.FailedRecords,
                    errorMessage = job.ErrorMessage,
                    resultsFile = job.ResultsFilePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to retrieve latest job",
                    message = ex.Message
                });
            }
        }
    }
}
