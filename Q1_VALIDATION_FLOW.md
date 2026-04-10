# Q1: Validation Flow - Frontend to Backend to Frontend

## High-Level Understanding

The FAB Batch Validator follows an **asynchronous, non-blocking validation pipeline**. Here's how the mechanism works step-by-step:

---

## 1. Frontend Initiates Validation

### **Where:** `Frontend/src/app/features/validation/validation-job.component.ts`
### **Action:** User clicks "Start Validation" button

```typescript
startValidation(): void {
  this.validationService.startValidation().subscribe({
    next: (job) => {
      this.currentJob = job;  // Display job info immediately
      this.isStarting = false;
    }
  });
}
```

### **What Happens:**
- Frontend sends HTTP POST request to `http://localhost:5000/api/validation/validate`
- Request body: **empty** (validation reads from a pre-configured input file)
- Frontend expects response immediately (NOT blocking)

---

## 2. Backend Receives Request (Non-Blocking Response)

### **Where:** `Backend/Controllers/ValidationController.cs` → `POST /api/validation/validate`

```csharp
[HttpPost("validate")]
public IActionResult Validate()
{
    // Step A: Validate input file has records
    var records = _inputReader.ReadRecords();
    
    // Step B: Create a NEW job (Pending state)
    var job = _jobManager.CreateJob();
    Console.WriteLine($"Created job {job.JobId} for {records.Count} records");
    
    // Step C: START validation on BACKGROUND THREAD (fire-and-forget)
    var worker = new AsyncValidationWorker(...);
    _ = Task.Run(async () => await worker.ExecuteValidationAsync(job.JobId));
    
    // Step D: RETURN IMMEDIATELY with job info (HTTP 202 Accepted)
    return Accepted(new
    {
        jobId = job.JobId,
        status = "Pending",
        inputRecordCount = records.Count,
        message = "Validation job queued"
    });
}
```

### **Why This Matters:**
- Backend returns **immediately** (HTTP 202 Accepted)
- Returns a **job ID** for tracking
- **Does NOT wait** for validation to complete
- Validation runs on the **thread pool** in the background

---

## 3. Backend Processes Records Asynchronously

### **Where:** `Backend/Services/AsyncValidationWorker.cs` → `ExecuteValidationAsync(jobId)`

### **The Worker Executes in Background:**

```csharp
public async Task ExecuteValidationAsync(string jobId)
{
    try
    {
        // 1. MARK JOB AS RUNNING
        _jobManager.MarkJobAsRunning(jobId);
        
        // 2. READ RECORDS from input file
        var records = _inputReader.ReadRecords();
        
        // 3. VALIDATE RECORDS (core business logic)
        var validationResult = await _validationService.ValidateRecordsAsync(records);
        //    ├─ For each record:
        //    │  ├─ Read PMID from Excel input
        //    │  ├─ Query agent API to validate
        //    │  ├─ Parse agent response
        //    │  └─ Store validation result (match type, confidence, etc.)
        
        // 4. SAVE RESULTS to JSON file
        await _resultRepository.SaveAsync(validationResult.ValidatedRecords);
        
        // 5. MARK JOB AS COMPLETED
        _jobManager.MarkJobAsCompleted(
            jobId,
            totalRecords: records.Count,
            successfulRecords: validationResult.SuccessfulRecords,
            failedRecords: validationResult.FailedRecords
        );
        
        Console.WriteLine($"Job {jobId} completed. Success: {successfulRecords}");
    }
    catch (Exception ex)
    {
        // 6. MARK JOB AS FAILED on error
        _jobManager.MarkJobAsFailed(jobId, ex.Message);
    }
}
```

### **Job State Progression:**
```
Pending → Running → Completed (or Failed)
```

---

## 4. Frontend Starts Polling for Status

### **Where:** `Frontend/src/app/core/services/validation.service.ts`

### **After Step 1 completes, Frontend Immediately starts an "endless loop":**

```typescript
startValidation(): Observable<BatchJob> {
    return this.apiService.post<ValidateResponse>('/validation/validate').pipe(
        map((response) => {
            const job: BatchJob = { jobId: response.jobId, ... };
            this.currentJobSubject.next(job);
            
            // START POLLING (every 2 seconds)
            this.startPollingJob(job.jobId);
            return job;
        })
    );
}

private startPollingJob(jobId: string, intervalMs: number = 2000): void {
    this.isPollingSubject.next(true);
    
    interval(intervalMs)  // Every 2 seconds
        .pipe(
            switchMap(() => this.getJobStatus(jobId))  // Send HTTP GET
        )
        .subscribe({
            next: (job) => {
                // Stop polling if job finished
                if (job.status === 'Completed' || job.status === 'Failed') {
                    this.stopPolling();
                }
            }
        });
}
```

### **What Happens:**
- Frontend sends `GET /api/validation/job/{jobId}` **every 2 seconds**
- Backend returns current job status (from `BackgroundJobManager`)
- Frontend updates job state in UI (progress bar, status text)
- Polling stops when job is **Completed** or **Failed**

---

## 5. Backend Provides Job Status

### **Where:** `Backend/Controllers/ValidationController.cs` → `GET /api/validation/job/{jobId}`

```csharp
[HttpGet("job/{jobId}")]
public IActionResult GetJobStatus(string jobId)
{
    var job = _jobManager.GetJob(jobId);  // Instant lookup (in-memory)
    
    return Ok(new
    {
        jobId = job.JobId,
        status = job.Status.ToString(),           // "Pending", "Running", "Completed", "Failed"
        totalRecords = job.TotalRecords,
        successfulRecords = job.SuccessfulRecords,
        failedRecords = job.FailedRecords,
        durationSeconds = job.DurationSeconds,
        completedAt = job.CompletedAt,
        errorMessage = job.ErrorMessage
    });
}
```

### **Status Lookup:**
- Job data is **stored in memory** by `BackgroundJobManager`
- Lookup is **O(1)** - extremely fast
- Also persisted to disk (`jobs-metadata.json`) for persistence

---

## 6. Frontend Auto-Refreshes Records When Job Completes

### **Where:** `Frontend/src/app/features/records/records-list.component.ts`

```typescript
ngOnInit(): void {
    // Auto-refresh records when validation completes
    this.validationService.currentJob$.subscribe(job => {
        if (job && job.status === 'Completed') {
            // Wait 1 second, then fetch records from backend
            setTimeout(() => {
                this.recordService.refreshRecords();
            }, 1000);
        }
    });
}

loadRecords(): void {
    this.recordService.getRecords().subscribe();
    // Calls: GET /api/records
}
```

### **What Happens:**
1. Validation job completes (status = "Completed")
2. Frontend detects completion via polling
3. Frontend waits 1 second for backend to finish saving results
4. Frontend sends `GET /api/records` to fetch validated records
5. Records appear in the UI table

---

## 7. Backend Returns Records

### **Where:** `Backend/Controllers/RecordsController.cs` → `GET /api/records`

```csharp
[HttpGet]
public async Task<ActionResult<List<ValidatedRecord>>> GetRecords()
{
    var records = await _resultRepository.LoadAsync();  // Load from JSON file
    return Ok(records);  // Return array of validated records
}
```

### **Data Flow:**
```
Backend /api/records
    ↓
JsonResultRepository.LoadAsync()
    ↓
Read results.json file
    ↓
Deserialize JSON → List<ValidatedRecord>
    ↓
Return to Frontend
```

---

## 8. Frontend Displays Records in Table

### **Where:** `Frontend/src/app/features/records/records-list.component.ts` + HTML template

```typescript
this.recordService.getRecords().subscribe({
    next: (records) => {
        this.records = records;  // Store in local state
        // Component renders: *ngFor="let record of records"
    }
});
```

### **UI Updates:**
- Records appear in an **HTML table**
- Each row shows: PMID, Match Type, Confidence Score, Title, etc.
- **Filters and search** can be applied (see Q2)

---

## Complete Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                    FRONTEND (Angular)                             │
│  (http://localhost:4200)                                         │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   │ 1. POST /api/validation/validate
                   ↓
┌──────────────────────────────────────────────────────────────────┐
│                     BACKEND (.NET)                                │
│  (http://localhost:5000)                                         │
│                                                                   │
│  ValidationController.Validate()                                 │
│  ├─ Create Job (ID: uuid)                                       │
│  ├─ Return 202 Accepted (immediately!)                          │
│  └─ Start AsyncValidationWorker on thread pool                  │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   │ 2. HTTP 202 + jobId
                   ↓
┌──────────────────────────────────────────────────────────────────┐
│  FRONTEND - Polling Starts (every 2 sec)                         │
│  GET /api/validation/job/{jobId}                                 │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   ↓ (Poll every 2 sec)
┌──────────────────────────────────────────────────────────────────┐
│                 BACKEND - Job Processing                          │
│                                                                   │
│  (Background Thread - AsyncValidationWorker)                     │
│  ├─ Read input Excel file (N records)                           │
│  ├─ For each record:                                            │
│  │  ├─ Query Agent API                                          │
│  │  ├─ Parse response                                           │
│  │  └─ Store validated data                                     │
│  ├─ Save results → results.json                                 │
│  └─ Mark job as COMPLETED                                       │
└──────────────────┬───────────────────────────────────────────────┘
                   │
    4. GET /api/validation/job/{jobId}
    Returns: status="Completed"
                   │
                   ↓
┌──────────────────────────────────────────────────────────────────┐
│  FRONTEND - Polling Stops                                         │
│  Detects job.status === "Completed"                             │
│  Wait 1 sec, then GET /api/records                              │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   │ 5. GET /api/records
                   ↓
┌──────────────────────────────────────────────────────────────────┐
│  BACKEND - RecordsController                                     │
│  Load validated records from results.json                        │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   │ 6. HTTP 200 + Array<ValidatedRecord>
                   ↓
┌──────────────────────────────────────────────────────────────────┐
│  FRONTEND - Display Records                                       │
│  ├─ Store records in RecordService                              │
│  ├─ Render table with records                                   │
│  ├─ Enable filters/search                                       │
│  └─ User sees validated records!                                │
└──────────────────────────────────────────────────────────────────┘
```

---

## Key Design Decisions

### **Why Async?**
- Validation can take **seconds to minutes** (network calls to agent API)
- If blocking, frontend would hang for entire duration
- Async lets backend process while frontend remains responsive

### **Why Polling?**
- Simple, no WebSockets needed
- Frontend can detect completion and trigger records refresh
- Every 2 seconds is reasonable overhead (configurable)

### **Why Job Manager?**
- Tracks job state in memory (`ConcurrentDictionary`)
- Fast O(1) lookups during polling
- Also saves to disk for persistence across restarts

### **Why Auto-Refresh Records?**
- Only refreshes when job completes (not on every poll)
- 1-second delay ensures backend has finished saving to disk
- Seamless UX: results appear automatically

---

## Data Structures Involved

### **Frontend:**
```typescript
interface BatchJob {
  jobId: string;
  status: "Pending" | "Running" | "Completed" | "Failed";
  totalRecords: number;
  successfulRecords: number;
  failedRecords: number;
  completedAt: Date;
  durationSeconds: number;
}

interface ValidatedRecord {
  inputPMID: string;
  matchedPMID: string;
  originalTitle: string;
  match_Extent: "Exact" | "Partial" | "No Match";
  confidenceScore: number;
  // ... other fields
}
```

### **Backend:**
```csharp
public class BatchJob
{
    public string JobId { get; set; }
    public JobStatus Status { get; set; }              // Pending, Running, Completed, Failed
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public string? ErrorMessage { get; set; }
}
```

---

## Summary

| Step | Component | Action | Blocking? |
|------|-----------|--------|-----------|
| 1 | Frontend (UI) | User clicks "Start Validation" | No |
| 2 | Frontend (Service) | POST /api/validation/validate | No |
| 3 | Backend (Controller) | Create job, return 202 | No |
| 4 | Backend (Worker) | Process records asynchronously | N/A (background thread) |
| 5 | Frontend (Service) | Poll GET /api/validation/job/{id} every 2 sec | No (non-blocking) |
| 6 | Backend (Controller) | Return job status | No |
| 7 | Backend (Worker) | Finish processing, save results, mark complete | N/A (background thread) |
| 8 | Frontend (Service) | Detect completion, GET /api/records | No |
| 9 | Backend (Controller) | Load results from JSON | No |
| 10 | Frontend (UI) | Display records in table | No |

The entire flow is **non-blocking, asynchronous, and user-friendly**.
