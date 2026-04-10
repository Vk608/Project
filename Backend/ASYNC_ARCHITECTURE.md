# Async Background Batch Processing Refactor

## Overview

The application has been refactored to support **asynchronous background validation** instead of synchronous blocking API calls. This allows the API to remain responsive while long-running validation jobs (up to ~1 hour) execute in the background.

### Key Principle
✅ **All existing validation logic remains unchanged** - Only the execution model has changed from synchronous to asynchronous.

---

## Architecture Changes

### Before (Synchronous)
```
POST /api/validation/validate
    ├─ Read Excel file
    ├─ Validate each record (1 hour+ wait)
    ├─ Save results
    └─ Return results to client
    ❌ Client blocks for entire duration
```

### After (Asynchronous Background Processing)
```
POST /api/validation/validate
    ├─ Create job (Pending)
    ├─ Start background task
    └─ Return immediately with Job ID (202 Accepted)
        ✅ HTTP request completes in milliseconds

Background Task (runs on thread pool)
    ├─ Update job status (Running)
    ├─ Read Excel file
    ├─ Validate each record
    ├─ Save results to JSON
    └─ Update job status (Completed/Failed)
```

---

## New Components

### 1. **BatchJob Model** (`Models/BatchJob.cs`)
Represents a validation job with tracking information:
- `JobId`: Unique identifier (GUID)
- `Status`: Pending → Running → Completed/Failed
- `CreatedAt`, `StartedAt`, `CompletedAt`: Timestamps
- `TotalRecords`, `SuccessfulRecords`, `FailedRecords`: Statistics
- `ErrorMessage`: Populated if job fails
- `ResultsFilePath`: Path to output JSON
- `DurationSeconds`: Computed job duration

### 2. **BackgroundJobManager** (`Services/BackgroundJobManager.cs`)
Manages job lifecycle and persistence:
- Creates new jobs
- Tracks job state (in-memory concurrent dictionary)
- Persists job metadata to `jobs-metadata.json`
- Retrieves jobs by ID or status
- Tracks the latest completed job
- Provides cleanup/maintenance utilities

### 3. **AsyncValidationWorker** (`Services/AsyncValidationWorker.cs`)
Executes validation asynchronously:
- Receives all dependencies (`ValidationService`, `JsonResultRepository`, etc.)
- Runs on thread pool (no external scheduler required)
- Reuses existing `ValidationService` as-is
- Updates job status throughout execution
- Handles errors gracefully and updates job status accordingly

### 4. **Enhanced ValidationController** (`Controllers/ValidationController.cs`)

#### `POST /api/validation/validate`
**Now returns immediately with job info:**
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "statusDisplay": "Job queued, waiting to start",
  "createdAt": "2025-04-09T10:30:00Z",
  "message": "Validation job queued. Check status with GET /api/validation/job/{jobId}",
  "inputRecordCount": 500
}
```

#### `GET /api/validation/job/{jobId}`
**Check status of a specific job:**
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Running",
  "statusDisplay": "Validation in progress",
  "createdAt": "2025-04-09T10:30:00Z",
  "startedAt": "2025-04-09T10:30:01Z",
  "completedAt": null,
  "durationSeconds": null,
  "totalRecords": 500,
  "successfulRecords": 0,
  "failedRecords": 0,
  "errorMessage": "",
  "resultsFile": "results.json"
}
```

#### `GET /api/validation/jobs` (Optional)
**List all jobs with optional status filter:**
```
GET /api/validation/jobs?status=Running
GET /api/validation/jobs?status=Completed
```

#### `GET /api/validation/latest` (Optional)
**Get the most recently completed job (regardless of success/failure):**
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "statusDisplay": "Job completed successfully",
  "totalRecords": 500,
  "successfulRecords": 490,
  "failedRecords": 10,
  "durationSeconds": 3600.5,
  "resultsFile": "results.json"
}
```

---

## Execution Flow

### 1. Job Creation
```csharp
// User makes request
POST /api/validation/validate

// Controller creates job
BatchJob job = _jobManager.CreateJob();
// Returns immediately with HTTP 202 (Accepted)

// Background task is started on thread pool
_ = Task.Run(async () => await worker.ExecuteValidationAsync(job.JobId));
```

### 2. Background Execution
```csharp
// Background thread executes validation
await worker.ExecuteValidationAsync(jobId)
  ├─ Read input records
  ├─ Call ValidationService.ValidateRecordsAsync() ← REUSED LOGIC
  ├─ Save results to JSON
  ├─ Update job status (Completed)
  └─ Persist job metadata
```

### 3. Result Retrieval
```csharp
// GET /api/records (unchanged behavior)
GET /api/records
// Returns latest completed validation results
```

---

## Backward Compatibility & Existing Code

✅ **ValidationService** - Unchanged. Still has:
- `ValidateRecordsAsync()` - Core validation logic
- Retry mechanism (1 automatic retry per record)
- Direct usage of ValidationPipeline components

✅ **JsonResultRepository** - Enhanced slightly:
- New method: `GetResultsFilePath()` (for job tracking)
- All existing methods work as before

✅ **RecordsController** - Completely unchanged:
- `GET /api/records` still returns latest results
- `GET /api/records/{index}` still works

✅ **ExcelInputReader, QueryTemplateBuilder, AgentApiClient, ResponseParser** - All unchanged

---

## Data Persistence

### Job Metadata (`jobs-metadata.json`)
Stores metadata for all jobs (for persistence across app restarts):
```json
[
  {
    "jobId": "550e8400-e29b-41d4-a716-446655440000",
    "status": 2,    // 0=Pending, 1=Running, 2=Completed, 3=Failed
    "createdAt": "2025-04-09T10:30:00Z",
    "startedAt": "2025-04-09T10:30:01Z",
    "completedAt": "2025-04-09T11:30:01Z",
    "totalRecords": 500,
    "successfulRecords": 490,
    "failedRecords": 10,
    "errorMessage": "",
    "resultsFilePath": "results.json"
  }
]
```

### Validation Results (`results.json`)
Still works exactly as before - contains validated records:
```json
[
  {
    "inputPMID": "12345678",
    "matchedPMID": "12345678",
    "confidenceScore": 0.95,
    "match_Extent": "Exact",
    "discrepancies_Logical": "",
    "discrepancies_Metadata": "",
    "summary": "Perfect match found in PubMed",
    "originalTitle": "Research Article Title"
  }
]
```

---

## Key Design Decisions

### ✅ Simple & Minimal
- No external queues (RabbitMQ, Redis, etc.)
- No schedulers (Hangfire, etc.)
- Uses .NET thread pool via `Task.Run()`
- Job state stored in JSON files

### ✅ Non-Breaking
- All existing components reused as-is
- Backward-compatible API structure
- `GET /records` returns latest results
- Configuration unchanged

### ✅ Scalable Within Limits
- Thread pool naturally handles multiple concurrent jobs
- In-memory job tracking with disk persistence
- File I/O is thread-safe

### ✅ Observable
- Job status tracking at all stages
- Detailed error messages captured
- Job history persisted
- Optional cleanup utility for old jobs

---

## Testing the Changes

### 1. Start a Validation Job
```bash
curl -X POST http://localhost:5000/api/validation/validate
```
Response (202 Accepted):
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "statusDisplay": "Job queued, waiting to start",
  "message": "Validation job queued. Check status with GET /api/validation/job/{jobId}"
}
```

### 2. Check Job Status
```bash
curl http://localhost:5000/api/validation/job/550e8400-e29b-41d4-a716-446655440000
```

### 3. List All Jobs
```bash
curl http://localhost:5000/api/validation/jobs
curl http://localhost:5000/api/validation/jobs?status=Completed
```

### 4. Get Latest Results
```bash
curl http://localhost:5000/api/records
```

---

## Configuration & Dependencies

### New Service Registration (in `Program.cs`)
```csharp
builder.Services.AddSingleton<BackgroundJobManager>();
```

### Required Namespaces
- `FABBatchValidator.Models` (BatchJob, JobStatus)
- `FABBatchValidator.Services` (BackgroundJobManager, AsyncValidationWorker)

---

## Monitoring & Maintenance

### Job Status at Different Stages
| Status | Meaning | Next Step |
|--------|---------|-----------|
| **Pending** | Job created, queued | Shortly transitions to Running |
| **Running** | Validation in progress | Wait for Completed or Failed |
| **Completed** | Success, results saved | Retrieve via GET /records |
| **Failed** | Error occurred | Check errorMessage in job status |

### Cleanup Old Jobs
```csharp
// Remove jobs older than 7 days
int removedCount = _jobManager.CleanupOldJobs(daysOld: 7);
```

---

## Common Workflows

### Workflow 1: One-Shot Validation
```
1. POST /api/validation/validate                 → Get jobId
2. Poll   GET /api/validation/job/{jobId}       → Wait for Completed
3. GET    /api/records                           → Retrieve results
```

### Workflow 2: Background Validation
```
1. POST /api/validation/validate                 → Get jobId immediately
2. [ Continue with other tasks... ]
3. Later: GET /api/records                       → Check if ready
```

### Workflow 3: Job History
```
1. GET /api/validation/jobs                      → List all jobs
2. GET /api/validation/latest                    → Latest job
3. GET /api/validation/job/{jobId}               → Specific job details
```

---

## Error Handling

### Validation Failures
- Individual record failures are captured in ValidationService (with retry)
- Job continues with remaining records
- Final job status shows success/failure counts
- Specific error captured if entire job fails

### Examples

**Job Failed (No input file)**
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Failed",
  "errorMessage": "No records found in input file"
}
```

**Partial Failure (Some records failed)**
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "totalRecords": 100,
  "successfulRecords": 95,
  "failedRecords": 5
}
```

---

## Performance Characteristics

### Before (Synchronous)
- HTTP request blocks for ~1 hour
- Client must wait for full validation
- Bad for long-running operations

### After (Asynchronous)
- HTTP request returns in milliseconds (202 Accepted)
- Validation runs in background
- Client can poll or receive notifications
- Multiple jobs can run concurrently

### Resource Usage
- Minimal overhead: job tracking + thread pool usage
- Memory: Proportional to number of concurrent jobs
- Storage: Job metadata (~100 bytes per job)

---

## Limitations & Considerations

1. **Job data persists in memory** - Restarting the app loses in-progress jobs (but completed jobs are restored from disk)
2. **Single machine** - No distributed job execution
3. **Basic error handling** - Failures stop the job (no retry at job level)
4. **No real-time notifications** - Clients must poll for status

---

## Future Enhancements (Optional)

1. **Webhooks** - Notify client when job completes
2. **Job Cancellation** - Allow stopping long-running jobs
3. **Job Prioritization** - Queue jobs by priority
4. **Progress Reporting** - Fine-grained progress per record
5. **Distributed Processing** - Multiple machines processing jobs
6. **Database Storage** - Replace JSON with database for job metadata

---

## Summary

✅ **What Changed**
- Validation now runs asynchronously in background
- API returns immediately with job ID
- New job status tracking & endpoints

✅ **What Stayed the Same**
- Core validation logic (ValidationService)
- Result storage (JsonResultRepository)
- Record retrieval (RecordsController)
- Configuration & input/output files

✅ **Key Benefits**
- API remains responsive (~1ms vs ~1 hour)
- Supports multiple concurrent validations
- Simple, no external infrastructure
- Observable job status & history
- Non-breaking changes to existing code
