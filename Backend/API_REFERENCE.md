# Quick API Reference - Async Batch Validation

## Endpoints

### 1. **Start a Validation Job** (Async)
```http
POST /api/validation/validate
Content-Type: application/json

```

**Response (202 Accepted):**
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

---

### 2. **Check Job Status**
```http
GET /api/validation/job/{jobId}
```

**Response (200 OK):**
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
  "successfulRecords": 212,
  "failedRecords": 3,
  "errorMessage": "",
  "resultsFile": "results.json"
}
```

**Status Values:**
- `Pending` - Job queued, waiting to start
- `Running` - Validation in progress
- `Completed` - Finished successfully
- `Failed` - Error occurred

---

### 3. **List All Jobs**
```http
GET /api/validation/jobs
GET /api/validation/jobs?status=Pending
GET /api/validation/jobs?status=Running
GET /api/validation/jobs?status=Completed
GET /api/validation/jobs?status=Failed
```

**Response (200 OK):**
```json
{
  "count": 3,
  "jobs": [
    {
      "jobId": "550e8400-e29b-41d4-a716-446655440000",
      "status": "Completed",
      "statusDisplay": "Job completed successfully",
      "createdAt": "2025-04-09T10:30:00Z",
      "completedAt": "2025-04-09T11:30:00Z",
      "durationSeconds": 3600.5,
      "successfulRecords": 490,
      "failedRecords": 10
    }
  ]
}
```

---

### 4. **Get Latest Job**
```http
GET /api/validation/latest
```

**Response (200 OK):**
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "statusDisplay": "Job completed successfully",
  "createdAt": "2025-04-09T10:30:00Z",
  "completedAt": "2025-04-09T11:30:00Z",
  "durationSeconds": 3600.5,
  "totalRecords": 500,
  "successfulRecords": 490,
  "failedRecords": 10,
  "errorMessage": "",
  "resultsFile": "results.json"
}
```

---

### 5. **Retrieve Validated Records** (Unchanged)
```http
GET /api/records
```

**Response (200 OK):**
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
  },
  ...
]
```

---

### 6. **Get Single Record by Index** (Unchanged)
```http
GET /api/records/{index}
```

---

## Usage Patterns

### Pattern 1: Fire and Forget
```csharp
// Start job and don't wait
var response = await client.PostAsync("/api/validation/validate", null);
var jobId = JsonSerializer.Deserialize<Job>(response.Content);
Console.WriteLine($"Job started: {jobId}");
// Later: check results via GET /api/records
```

### Pattern 2: Poll Until Complete
```csharp
// Start job
var response = await client.PostAsync("/api/validation/validate", null);
var job = JsonSerializer.Deserialize<Job>(response.Content);

// Poll status
while (true)
{
    var statusResponse = await client.GetAsync($"/api/validation/job/{job.JobId}");
    var status = JsonSerializer.Deserialize<JobStatus>(statusResponse.Content);
    
    if (status.Status == "Completed" || status.Status == "Failed")
    {
        Console.WriteLine($"Job finished: {status.Status}");
        break;
    }
    
    await Task.Delay(5000); // Poll every 5 seconds
}
```

### Pattern 3: Check Periodically
```javascript
// Start validation (async)
fetch('/api/validation/validate', { method: 'POST' })
  .then(r => r.json())
  .then(data => {
    console.log(`Job ${data.jobId} started`);
    
    // Check periodically
    setInterval(async () => {
      const status = await fetch(`/api/validation/job/${data.jobId}`).then(r => r.json());
      if (status.status === 'Completed') {
        // Reload results
        const results = await fetch('/api/records').then(r => r.json());
        console.log('Latest results:', results);
      }
    }, 5000); // Check every 5 seconds
  });
```

---

## Migration Guide (If You Had Old Code)

The old synchronous endpoint:
```csharp
// OLD: POST would block for ~1 hour
POST /api/validation/validate
// Response: { "success": true, "successfulRecords": 490, ... }
```

Has been replaced with async:
```csharp
// NEW: POST returns immediately
POST /api/validation/validate
// Response: { "jobId": "...", "status": "Pending", ... } (202 Accepted)

// Then check status:
GET /api/validation/job/{jobId}
// Or get results:
GET /api/records
```

**Minimal client code change:**
```csharp
// OLD CODE (blocking)
var result = await validationApi.PostAsyncAwait("/validate");
var records = result.ValidatedRecords;

// NEW CODE (async)
var job = await validationApi.PostAsync("/validate");
// Poll or wait...
var jobStatus = await validationApi.GetAsync($"/job/{job.JobId}");
// Or just get latest results:
var records = await validationApi.GetAsync("/records");
```

---

## Common Questions

### Q: How long does validation take?
**A:** Same as before (~1 hour for large datasets). The difference is the API returns immediately instead of blocking.

### Q: Can I run multiple validations simultaneously?
**A:** Yes! Each POST starts a new background job. They run concurrently (limited by thread pool).

### Q: What happens if the app restarts?
**A:** Completed jobs are restored from `jobs-metadata.json`. In-progress jobs are lost, but the results from the previous completed job remain in `results.json`.

### Q: How do I know when validation is done?
**A:** Poll `GET /api/validation/job/{jobId}` until status is `Completed` or `Failed`.

### Q: Can I cancel a running job?
**A:** Not yet. This would be a future enhancement.

### Q: Where are results stored?
**A:** In `results.json` (same as before). Accessed via `GET /api/records`.

### Q: Is there any memory usage overhead?
**A:** Minimal - just job metadata in memory. Each job is ~100 bytes. Disk usage is similar.

---

## Error Handling

### No input file
```json
{
  "error": "Failed to queue validation job",
  "message": "No records found in input file"
}
```

### After job fails
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Failed",
  "statusDisplay": "Job failed",
  "errorMessage": "Unable to connect to FAB Agent"
}
```

### Partial record failures (job still completes)
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

## HTTP Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| **202** | Job accepted and queued | POST /validate |
| **200** | Request successful | GET /job/{id} |
| **400** | Bad request | No input file |
| **404** | Not found | Job doesn't exist |
| **500** | Server error | FAB Agent unavailable |

---

## Example: Full Workflow in JavaScript

```javascript
async function validateAndWait() {
  try {
    // 1. Start validation
    console.log('Starting validation...');
    const startResponse = await fetch('/api/validation/validate', { method: 'POST' });
    const { jobId } = await startResponse.json();
    console.log(`Job started: ${jobId}`);

    // 2. Poll until complete
    let completed = false;
    let jobStatus;
    while (!completed) {
      const response = await fetch(`/api/validation/job/${jobId}`);
      jobStatus = await response.json();
      
      console.log(`Status: ${jobStatus.statusDisplay}`);
      console.log(`  Progress: ${jobStatus.successfulRecords}/${jobStatus.totalRecords}`);
      
      if (jobStatus.status === 'Completed' || jobStatus.status === 'Failed') {
        completed = true;
      } else {
        await new Promise(resolve => setTimeout(resolve, 5000)); // Wait 5s
      }
    }

    // 3. Fetch results
    const recordsResponse = await fetch('/api/records');
    const records = await recordsResponse.json();
    
    console.log(`✓ Validation complete!`);
    console.log(`  Total: ${records.length}`);
    console.log(`  Job duration: ${jobStatus.durationSeconds}s`);
    
    return records;

  } catch (error) {
    console.error('Validation failed:', error);
    throw error;
  }
}

// Usage
validateAndWait().then(records => console.log(records));
```

---

## Files Modified/Added

### New Files
- `Models/BatchJob.cs` - Job tracking model
- `Services/BackgroundJobManager.cs` - Job lifecycle management
- `Services/AsyncValidationWorker.cs` - Background execution logic
- `ASYNC_ARCHITECTURE.md` - Detailed architecture documentation
- `API_REFERENCE.md` - This file

### Modified Files
- `Controllers/ValidationController.cs` - Now returns job IDs, added status endpoints
- `Storage/JsonResultRepository.cs` - Added `GetResultsFilePath()` method
- `Program.cs` - Added BackgroundJobManager registration

### Unchanged Files
- `ValidationService.cs` - Core logic untouched
- `RecordsController.cs` - Fully backward compatible
- All other services, models, and utilities remain the same
