using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FABBatchValidator.Excel;
using FABBatchValidator.Models;
using FABBatchValidator.Storage;

namespace FABBatchValidator.Services
{
    /// <summary>
    /// Encapsulates the asynchronous validation work.
    /// Receives all dependencies and job tracking information.
    /// Executes the validation pipeline without blocking the HTTP request thread.
    /// </summary>
    public class AsyncValidationWorker
    {
        private readonly ValidationService _validationService;
        private readonly JsonResultRepository _resultRepository;
        private readonly ExcelInputReader _inputReader;
        private readonly BackgroundJobManager _jobManager;

        public AsyncValidationWorker(
            ValidationService validationService,
            JsonResultRepository resultRepository,
            ExcelInputReader inputReader,
            BackgroundJobManager jobManager)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        }

        /// <summary>
        /// Execute the full validation pipeline for a given job.
        /// This method is called from a background thread pool task.
        /// </summary>
        public async Task ExecuteValidationAsync(string jobId, string inputFilePath = null)
        {
            try
            {
                Console.WriteLine($"[AsyncValidationWorker] Starting validation for job {jobId}");
                _jobManager.MarkJobAsRunning(jobId);

                // Step 1: Read input records
                Console.WriteLine($"[AsyncValidationWorker] Reading input records...");
                var records = _inputReader.ReadRecords(inputFilePath);

                if (records.Count == 0)
                {
                    throw new InvalidOperationException("No records found in input file");
                }

                // Step 2: Validate records using the existing ValidationService
                Console.WriteLine($"[AsyncValidationWorker] Validating {records.Count} records...");
                var validationResult = await _validationService.ValidateRecordsAsync(records);

                // Step 3: Store results
                Console.WriteLine($"[AsyncValidationWorker] Saving results for job {jobId}...");
                string jobFilePath = await _resultRepository.SaveForJobAsync(jobId, validationResult.ValidatedRecords);

                // Step 4: Mark job as completed
                _jobManager.MarkJobAsCompleted(
                    jobId,
                    totalRecords: records.Count,
                    successfulRecords: validationResult.SuccessfulRecords,
                    failedRecords: validationResult.FailedRecords,
                    resultsFilePath: jobFilePath
                );

                Console.WriteLine($"[AsyncValidationWorker] Job {jobId} completed successfully. " +
                    $"Success: {validationResult.SuccessfulRecords}, Failed: {validationResult.FailedRecords}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsyncValidationWorker] Job {jobId} failed with error: {ex.Message}");
                Console.WriteLine($"[AsyncValidationWorker] Stack trace: {ex.StackTrace}");

                _jobManager.MarkJobAsFailed(jobId, ex.Message);
            }
        }
    }
}
