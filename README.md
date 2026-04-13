# PubMed Data Sync Validation - Full Stack Solution

> **A system for detecting discrepancies between internal article metadata and the official PubMed dataset using AI-powered validation.**

---

## Table of Contents

1. [Business Problem](#business-problem)
2. [High-Level Architecture](#high-level-architecture)
3. [Frontend Overview](#frontend-overview)
4. [Backend Overview](#backend-overview)
5. [Utility Worker & Background Jobs](#utility-worker--background-jobs)
6. [Agent Integration](#agent-integration)
7. [Data Flow Pipeline](#data-flow-pipeline)
8. [Storage](#storage)
9. [API Behavior](#api-behavior)
10. [Limitations](#limitations)
11. [Possible Improvements](#possible-improvements)
12. [Tech Stack](#tech-stack)

---

## Business Problem

### The Challenge
Your internal editorial system stores article metadata (PMIDs, titles, authors, journals, etc.) that may **drift out of sync** with the official PubMed dataset over time. This happens due to:
- Data corrections in PubMed
- Incomplete initial imports
- External dataset updates

### Why It Matters
- Editors need confidence that internal records match authoritative PubMed data
- Finding mismatches manually is time-consuming and error-prone
- Lost PMIDs or metadata changes can impact workflows

### The Solution
A **batch validation system** that:
1. Takes internal article records as input (Excel file)
2. Queries each record against a PubMed search agent
3. Flags discrepancies (missing records, field mismatches)
4. Provides a visual dashboard to review results

---

## High-Level Architecture

```
┌─────────────┐
│  Frontend   │ (Angular)
│  Dashboard  │
└──────┬──────┘
       │ HTTP
       ↓
┌─────────────────────────────────────────┐
│   Backend API (ASP.NET Core)            │
│  ┌─────────────────────────────────────┐│
│  │ POST /validate → Queue Job (202)    ││
│  │ GET /job/{id} → Check Status (200)  ││
│  │ GET /records → Get Results (200)    ││
│  └─────────────────────────────────────┘│
└──────┬────────────────────────────────────┘
       │
       │ Background Task
       ↓
┌─────────────────────────────────────────┐
│   AsyncValidationWorker                  │
│  ┌─────────────────────────────────────┐│
│  │ 1. Read Excel (input records)       ││
│  │ 2. Loop each record                 ││
│  │ 3. Send to Agent API (1-at-a-time) ││
│  │ 4. Parse response                   ││
│  │ 5. Store in results.json            ││
│  └─────────────────────────────────────┘│
└──────┬────────────────────────────────────┘
       │
       │ HTTP
       ↓
┌─────────────────────────────────────────┐
│   FAB Agent (PubMed Search)              │
│  ┌─────────────────────────────────────┐│
│  │ Query: "{PMID: X, other params}"    ││
│  │ Response: Confidence + matched data││
│  └─────────────────────────────────────┘│
└─────────────────────────────────────────┘
       │
       ↓ Results
   Stored in
  results.json / Excel

┌─────────────────────────────────────────┐
│   Frontend (Refresh)                    │
│   Displays validated records with:       │
│   - Confidence scores                    │
│   - Match type (exact/semantic)          │
│   - Discrepancy flags                    │
└─────────────────────────────────────────┘
```

---

## Frontend Overview

**Location:** `/Frontend` (Angular 17, Standalone Components)

### Main Pages

#### 1. **Records List Dashboard** (`records-list.component.ts`)
- Displays all validated results in a table
- **Features:**
  - Pagination (10 records per page)
  - Filtering by match type (Exact, Partial, Semantic)
  - Confidence score range slider
  - Search by PMID or title
  - Sort columns (PMID, confidence, match type)
  - Expandable rows for details

#### 2. **Validation Job Manager** (`validation-job.component.ts`)
- Sidebar panel to start/monitor batch jobs
- **Features:**
  - Start validation button
  - Real-time progress bar
  - Job status display (Pending → Running → Completed)
  - Success/failure count
  - Duration tracking

### Data Flow in Frontend

```
Component Init
    ↓
Load Records (GET /api/records)
    ↓
Store in RecordService (BehaviorSubject)
    ↓
Apply Filters/Sort (In-memory)
    ↓
Paginate (10 per page)
    ↓
Render Table
```

### State Management

**RecordService** - Central state manager using RxJS:
```typescript
- filteredRecords$ (BehaviorSubject)     // All valid records after filters
- isLoading$ (BehaviorSubject)            // Loading indicator
- error$ (BehaviorSubject)                // Error messages
```

**ValidationService** - Job tracking:
```typescript
- currentJob$ (BehaviorSubject)          // Current running job
- isPolling$ (BehaviorSubject)           // Polling status
```

### Component Communication
- Services → Observable patterns (pub/sub)
- HTTP calls → Cached automatically
- Auto-refresh on validation completion

---

## Backend Overview

**Location:** `/Backend` (.NET 10.0, ASP.NET Core Web API)

### Architecture Layers

#### 1. **Controllers Layer**
```
ValidationController
├─ POST /api/validation/validate     (Start job - returns 202)
├─ GET  /api/validation/job/{id}     (Check status - returns 200)
├─ GET  /api/validation/jobs         (List all jobs - returns 200)
├─ GET  /api/validation/latest       (Latest job - returns 200)
└─ GET  /api/records                 (Get results - returns 200)
```

#### 2. **Services Layer**

**ValidationService** - Core validation logic
- Processes each record sequentially
- Builds query from record fields
- Calls agent API
- Parses response
- Retries failed records once
- Returns ValidationServiceResult

**BackgroundJobManager** - Job lifecycle
- Creates jobs in `Pending` state
- Updates job status (Pending → Running → Completed/Failed)
- Persists jobs to `jobs-metadata.json` on disk
- Provides job lookup by ID

**AsyncValidationWorker** - Background execution
- Runs on thread pool (not blocking HTTP request)
- Orchestrates: Read → Validate → Store
- Updates job metadata as it progresses

#### 3. **Agent Integration**
**AgentApiClient** - HTTP wrapper
- Sends formatted query to FAB agent
- Handles timeouts & retries
- Parses JSON response
- Throws exceptions on failures

#### 4. **Data Access**
**ExcelInputReader** - Reads input.xlsx
**ExcelOutputWriter** - Writes output.xlsx
**JsonResultRepository** - Persists results to results.json

---

## Utility Worker & Background Jobs

### Why Background Processing?

**Before:** POST /validate → Wait 30-60 min for results → HTTP timeout risk ❌

**After:** POST /validate → Return JobId immediately → Client polls → Results ready ✅

### How It Works

1. **User clicks "Start Validation"** in frontend
2. **Backend receives POST request**
   - Generates unique `JobId`
   - Creates `BatchJob` record (status = `Pending`)
   - Immediately returns `202 Accepted` with JobId
   - HTTP connection closes

3. **Background Worker starts on thread pool** (fire-and-forget)
   ```csharp
   _ = Task.Run(async () => await worker.ExecuteValidationAsync(jobId));
   ```
   - Updates job status → `Running`
   - Reads input records from Excel
   - Loops each record (sequential, one-at-a-time)
   - Updates job progress as it goes
   - Stores results when complete
   - Updates job status → `Completed` or `Failed`

4. **Frontend polls for status**
   - `GET /api/validation/job/{jobId}` every 2-5 seconds
   - Shows progress bar
   - When status = `Completed`, loads results

### Job State Persisted to Disk

**File:** `jobs-metadata.json`
```json
{
  "jobs": [
    {
      "jobId": "550e8400-e29b-41d4-a716-446655440000",
      "status": "Completed",
      "totalRecords": 500,
      "successfulRecords": 485,
      "failedRecords": 15,
      "createdAt": "2025-04-09T10:30:00Z",
      "startedAt": "2025-04-09T10:30:01Z",
      "completedAt": "2025-04-09T11:30:00Z"
    }
  ]
}
```

### Processing Model

- **Sequential Processing:** Records are validated one-at-a-time
- **Retry Logic:** If a record fails, retry once automatically
- **No External Queue:** Uses .NET thread pool (simple, built-in)

---

## Agent Integration

### What is the FAB Agent?

A **PubMed search engine** powered by FAB (Flexible Agent Building). It:
- Indexes PubMed XML data
- Supports multi-parameter search (PMID, title, authors, journal, etc.)
- Returns confidence scores (0-1 scale)
- Identifies exact vs. semantic matches

### How Backend Communicates

**AgentApiClient** sends HTTP POST requests:

```http
POST https://agent-api.example.com/query
Headers:
  x-user-id: YOUR_USER_ID
  x-authentication: api-key YOUR_API_KEY
  Content-Type: application/json

Body:
{
  "input": {
    "query": "PMID: 12345678; Abstract: 'tumor suppression'"
  }
}
```

**Agent Response:**
```json
{
  "output": {
    "matchedPMID": "12345678",
    "confidenceScore": 0.95,
    "matchExtent": "Exact",
    "title": "...",
    "authors": [...],
    "journal": "...",
    "publicationYear": 2024
  }
}
```

### Query Building

**QueryTemplateBuilder** constructs queries dynamically:
```csharp
var query = $"PMID: {record.PMID}; Title: {record.Title}; Abstract: {record.Abstract}";
```

### Response Parsing

**ResponseParser** extracts data:
- Confidence score (float 0-1)
- Match extent (Exact, Partial, Semantic)
- Matched PMID (same or different)
- Confidence-based classification

---

## Data Flow Pipeline

### End-to-End Request Journey

```
1. USER ACTION
   └─ Frontend: Click "Start Validation"

2. API CALL
   └─ POST /api/validation/validate (immediate return)

3. BACKEND ACCEPTS
   └─ Create Job with status=Pending
   └─ Return 202 + JobId
   └─ Start background worker (fire-and-forget)

4. BACKGROUND WORKER RUNS
   └─ Update status → Running
   └─ Read input.xlsx (500 records)
   └─ FOR each record:
      ├─ Build Query: "PMID: X; Title: Y; Authors: Z"
      ├─ Call Agent API: POST query
      ├─ Parse Agent Response:
      │  ├─ Extract confidenceScore
      │  ├─ Extract matchExtent (Exact/Partial/Semantic)
      │  └─ Compare PMID (match or mismatch)
      ├─ Create ValidatedRecord
      ├─ IF confidence < threshold: Mark as failed/flagged
      └─ IF error: Retry once, then mark failed
   
   └─ Store results
      ├─ Save to results.json
      ├─ Export to output.xlsx
   
   └─ Update status → Completed

5. FRONTEND POLLS
   └─ GET /api/validation/job/{jobId} every 3 seconds
   └─ Display progress: "485 / 500 passed"
   └─ When status=Completed: Fetch results

6. FRONTEND DISPLAYS
   └─ GET /api/records
   └─ Render table with:
      ├─ Input PMID vs Matched PMID
      ├─ Confidence score (color-coded)
      ├─ Match extent badge
      └─ Flag for mismatches
```

### Validation Logic

For each record:
1. **Agent returns match + confidence score**
2. **ValidatedRecord created with:**
   - `inputPMID` (from input file)
   - `matchedPMID` (from agent)
   - `confidenceScore` (agent's confidence)
   - `match_Extent` (Exact / Partial / Semantic)
   - `title`, `authors`, `journal`, etc.

3. **Discrepancy Detection:**
   - PMID mismatch: `inputPMID != matchedPMID`
   - Low confidence: `confidenceScore < 0.7`
   - Missing record: Agent returns error

---

## Storage

### Why Excel?

✅ **Easy for clients** - Open in Excel, no special tools  
✅ **Non-technical users** - Can view/export results  
✅ **Formatted output** - Headers, colors, formulas possible  

❌ **Not scalable** - Limited to ~1M rows  
❌ **Not queryable** - Can't filter without re-reading  
❌ **File-based** - No concurrent access control  

### Input File: `input.xlsx`

```
| PMID    | Title                | Authors          | Journal    |
|---------|----------------------|------------------|------------|
| 12345   | Tumor Suppression    | John Smith, ...  | Nature Med |
| 56789   | Gene Therapy         | Jane Doe, ...    | JAMA       |
```

### Output File: `results.json`

```json
{
  "validatedRecords": [
    {
      "inputPMID": "12345",
      "matchedPMID": "12345",
      "title": "Tumor Suppression",
      "confidenceScore": 0.95,
      "match_Extent": "Exact",
      "authors": "John Smith, ...",
      "journal": "Nature Med",
      "publicationYear": 2024
    }
  ]
}
```

### Frontend Storage

**Angular In-Memory:**
- `RecordService` stores filtered records in `BehaviorSubject` (RAM only)
- Survives page refresh if data is cached by HTTP interceptor
- Limit: ~5000 records comfortable in RAM

**For 1000+ Records - Options:**
1. **LocalStorage** - Browser storage, ~5-10MB limit
   ```typescript
   localStorage.setItem('records', JSON.stringify(records));
   ```
2. **IndexedDB** - Client-side database, ~50-100MB+ available
   ```typescript
   db.open().add(records);  // Via idb library
   ```
3. **Virtual Scrolling** - Render only visible rows (RxJS + CDK)
   ```html
   <cdk-virtual-scroll-viewport>
     <tr *cdkVirtualFor="let record of records"></tr>
   </cdk-virtual-scroll-viewport>
   ```

### Backend Storage

**Job Metadata:** `jobs-metadata.json`
- Persists across server restarts
- Fast lookup by jobId
- Human-readable

**Validation Results:** `results.json`
- Written by `AsyncValidationWorker`
- Read by `RecordsController` for GET /api/records
- ~1MB per 1000 records

---

## API Behavior

### HTTP Status Codes & Semantics

#### 1. **Start Validation: 202 Accepted**
```http
POST /api/validation/validate
→ 202 Accepted
{
  "jobId": "550e8400-...",
  "status": "Pending",
  "message": "Validation job queued. Check status with GET /api/validation/job/{jobId}"
}
```

**Why 202?**
- Indicates request accepted but NOT completed yet
- Tells client to check status later
- Prevents timeout errors

---

#### 2. **Check Job Status: 200 OK**
```http
GET /api/validation/job/550e8400-...
→ 200 OK
{
  "jobId": "550e8400-...",
  "status": "Running",
  "successfulRecords": 212,
  "failedRecords": 3,
  "totalRecords": 500
}
```

**Polling Loop (Frontend):**
```typescript
checkStatus() {
  interval(3000)  // Every 3 seconds
    .pipe(
      switchMap(jobId => this.http.get(`/api/validation/job/${jobId}`)),
      takeUntil(jobCompleted)  // Stop when status=Completed
    )
    .subscribe(job => this.updateProgressBar(job));
}
```

---

#### 3. **Get Results: 200 OK**
```http
GET /api/records
→ 200 OK
{
  "validatedRecords": [
    {
      "inputPMID": "12345",
      "matchedPMID": "12345",
      "confidenceScore": 0.95,
      ...
    }
  ]
}
```

---

#### 4. **Error Cases: 400 / 500**
```http
POST /api/validation/validate (no input file)
→ 400 Bad Request
{ "error": "No records found in input file" }

GET /api/validation/job/INVALID_ID
→ 404 Not Found
{ "error": "Job not found" }

Agent API timeout
→ Job status updated to Failed with error message
→ Frontend displays error to user
```

---

### Polling Strategy

**Frontend polls for job status:**

```
Interval: 3 seconds
Max duration: Allows up to 24hrs of polling
If status in [Completed, Failed]:
  └─ Stop polling
  └─ Fetch results
  └─ Display on dashboard
```

---

## Limitations

### 1. **Sequential Processing**
- **Problem:** Processes one record at a time
- **Impact:** 1000 records = 2-5 seconds each = 33-83 minutes total
- **Risk:** HTTP timeout if agent is slow
- **Data:** Only tested with small files (≤50 records)

### 2. **Excel Storage Limitations**
- **Max rows:** ~1M per sheet (practical: 100K)
- **No concurrent access:** File locks if another process reads/writes
- **No queries:** Must read entire file to filter
- **Legacy format:** .xlsx is not ideal for modern APIs

### 3. **Agent Reliability**
- **Timeout risk:** If agent is slow, job times out after 30 min
- **No batching:** One HTTP call per record (overhead)
- **No fallback:** If agent down, entire validation fails
- **Retry limit:** Only 1 automatic retry per record

### 4. **Frontend Performance (1000+ Records)**
- **DOM rendering:** Table with 1000 rows = slower
- **Filter on 1000 items:** No debouncing = lag on search
- **Memory:** All records in RAM = ~5-10MB per 1000 records
- **No pagination on backend:** All data fetched at once

### 5. **No Authentication/Authorization**
- Backend API accessible without credentials
- No role-based access (all users see all records)
- Job IDs are GUIDs but guessable if predictable

### 6. **State Not Thread-Safe at Scale**
- `ConcurrentDictionary` is thread-safe but
- If multiple instances of backend run, jobs duplicated in memory
- No distributed job queue (local only)

---

## Possible Improvements

### 1. **Parallel Processing (🔴 High Priority)**
**Problem:** 1000 records = 60+ minutes  
**Solution:** Process 5-10 records in parallel
```csharp
var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };
await Parallel.ForEachAsync(records, parallelOptions, async (record, ct) => {
    var validated = await ProcessRecordWithRetryAsync(record);
    result.AddValidatedRecord(validated);
});
```
**Impact:** 60 min → 10-15 min ✅

---

### 2. **Batch Agent Queries (🟡 Medium Priority)**
**Current:** 1000 records = 1000 HTTP calls  
**Better:** 1000 records = 100 HTTP calls (batch 10 per request)
```csharp
var batch = records.Chunk(10);
foreach (var chunk in batch) {
    var results = await agent.QueryBatch(chunk);
    // Process results
}
```
**Impact:** Network overhead reduced by 90% ✅

---

### 3. **Database Instead of Excel (🟡 Medium Priority)**
Replace Excel with SQL database:
- PostgreSQL / SQL Server
- Real-time concurrent access
- Queryable (no full-file reads)
- Better for 100K+ records
- Enables incremental updates

```csharp
INSERT INTO ValidatedRecords (inputPMID, matchedPMID, confidence)
SELECT * FROM staging_results;
```

---

### 4. **File Upload Feature** (🟢 Low Priority)
Currently: Hardcoded input.xlsx path  
Better: Let users upload their own file
```html
<input type="file" (change)="onFileUpload($event)">
```
- Multipart form upload
- Validate file format (Excel, CSV, JSON)
- Scan for viruses

---

### 5. **Real-Time Updates via WebSocket** (🟡 Medium Priority)
Replace polling with push updates:
```typescript
socket.on('job-progress', (job) => {
  this.currentJob$.next(job);  // Instant update
});
```
**Impact:** No polling overhead, instant UI updates ✅

---

### 6. **Caching & Incremental Validation** (🟡 Medium Priority)
- Cache agent responses (same PMID = same result)
- Only validate records not seen before
- Track validation history per record

```csharp
var cached = cache.Get("PMID:12345");
if (cached != null) return cached;  // Skip agent call
```

---

### 7. **Advanced Frontend Pagination** (🟢 Low Priority)
Already implemented but could add:
- Server-side pagination (backend returns page N)
- Virtual scrolling (render only visible rows)
- Lazy loading (fetch next page on scroll)

---

### 8. **Job Retry & Resume** (🟡 Medium Priority)
Currently: If job fails, must restart  
Better: Resume from failed record
```csharp
var failedRecords = job.GetFailedRecords();
await ResumeValidationAsync(jobId, failedRecords);
```

---

### 9. **Monitoring & Logging** (🟢 Low Priority)
Add observability:
- Job execution time per record (identify slowdowns)
- Agent response times
- Error rates by type
- Metrics dashboard

---

### 10. **Authentication & Multi-Tenancy** (🔴 High Priority)
Add security:
```csharp
[Authorize(Roles = "Editor")]
public IActionResult StartValidation() { ... }

// Isolate data per tenant
var records = db.Records.Where(r => r.TenantId == userId).ToList();
```

---

## Tech Stack

### Frontend

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Framework** | Angular | 17.0.0 | SPA framework (standalone components) |
| **Language** | TypeScript | 5.2 | Type-safe JavaScript |
| **State Management** | RxJS | 7.8.0 | Reactive programming (observables) |
| **HTTP Client** | Angular HttpClient | Built-in | API communication |
| **Forms** | Angular Forms | Built-in | Reactive forms for filters |
| **Styling** | SCSS | Built-in | Component-scoped styles |
| **Build Tool** | Angular CLI | 17.0.0 | Dev server, build, test |

### Backend

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Runtime** | .NET | 10.0 | Modern C# runtime |
| **Framework** | ASP.NET Core | 10.0 | Web API framework |
| **Language** | C# | Latest | Type-safe backend |
| **HTTP** | HttpClient | Built-in | Call agent API |
| **JSON** | System.Text.Json | Built-in | Serialization |
| **Excel** | EPPlus | 7.1.3 | Read/write Excel files |
| **Job Queue** | Thread Pool (Task) | Built-in | Background execution |
| **Async** | async/await | C# | Non-blocking I/O |

### Integration

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Agent** | FAB (OpenAI-based) | PubMed search + scoring |
| **API Communication** | REST HTTP | Backend ↔ Frontend |
| **Authentication** | API Keys | Backend ↔ Agent |
| **Local Storage** | JSON files | Job metadata, results |
| **Input/Output** | Excel (.xlsx) | Data import/export |

### Development Tools

```bash
# Frontend
npm install          # Install dependencies
ng serve            # Dev server (localhost:4200)
ng build --prod     # Production build

# Backend
dotnet restore      # Install NuGet packages
dotnet build        # Compile
dotnet run          # Run server (localhost:5000)
```

---

## Quick Links

- **Frontend README:** [Frontend/README.md](Frontend/README.md)
- **Backend Setup:** [Backend/QUICK_START.md](Backend/QUICK_START.md)
- **Architecture Details:** [Backend/ASYNC_ARCHITECTURE.md](Backend/ASYNC_ARCHITECTURE.md)
- **API Reference:** [Backend/API_REFERENCE.md](Backend/API_REFERENCE.md)
- **Problem Statement:** [Backend/ProblemStatement.md](Backend/ProblemStatement.md)

---

## Getting Started

### Backend Setup
```bash
cd Backend
dotnet restore
dotnet run
# Server runs on http://localhost:5000
```

### Frontend Setup
```bash
cd Frontend
npm install
ng serve
# Dashboard runs on http://localhost:4200
```

### First Validation
1. Place your data in `Backend/input.xlsx`
2. Click "Start Validation" button
3. Wait for job to complete
4. View results on dashboard

---

**Last Updated:** April 2026  
**Status:** Production Ready (with limitations noted above)
