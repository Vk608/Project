# Backend-Frontend API Integration Guide

This document provides a complete mapping between backend ASP.NET Core APIs and frontend Angular service calls.

## Backend Overview

**Backend Location:** `c:\Users\vinay\Downloads\FullStack\Backend\`
**Running On:** `http://localhost:5000`
**Framework:** ASP.NET Core Web API (Async Background Processing)

## API Endpoints Reference

### 1. Start Validation Job
**Backend Method:** `ValidationController.ValidateAsync()`
```csharp
POST /api/validation/validate
Request: { excelFilePath: string }
Response: { jobId: string, status: string, createdAt: DateTime }
```

**Frontend Service Call:**
```typescript
// In ValidationService
startValidation(): Observable<ValidateResponse> {
  return this.apiService.post<ValidateResponse>('/validation/validate', {
    excelFilePath: 'path/to/file.xlsx'
  });
}
```

**Frontend Usage:**
```typescript
// In validation-job.component.ts
startValidation() {
  this.isStarting = true;
  this.validationService.startValidation().subscribe({
    next: (response) => {
      this.currentJob = response;
      this.validationService.pollJobStatus(response.jobId); // Start polling
      this.isStarting = false;
    },
    error: (err) => {
      this.error = 'Failed to start validation job';
      this.isStarting = false;
    }
  });
}
```

---

### 2. Get Job Status
**Backend Method:** `ValidationController.GetJobStatusAsync(jobId)`
```csharp
GET /api/validation/job/{jobId}
Response: {
  jobId: string,
  status: "Pending" | "Running" | "Completed" | "Failed",
  createdAt: DateTime,
  startedAt?: DateTime,
  completedAt?: DateTime,
  durationSeconds?: number,
  totalRecords: number,
  successfulRecords: number,
  failedRecords: number,
  errorMessage?: string
}
```

**Frontend Service Call:**
```typescript
// In ValidationService
getJobStatus(jobId: string): Observable<JobStatusResponse> {
  return this.apiService.get<JobStatusResponse>(`/validation/job/${jobId}`);
}

// Polling mechanism with auto-stop
pollJobStatus(jobId: string, pollInterval = 2000): void {
  this.isPolling$.next(true);
  
  interval(pollInterval)
    .pipe(
      switchMap(() => this.getJobStatus(jobId)),
      takeUntil(this.destroy$),
      tap(response => {
        this.currentJob$.next(response);
        // Auto-stop polling when job completes
        if (['Completed', 'Failed'].includes(response.status)) {
          this.isPolling$.next(false);
        }
      })
    )
    .subscribe();
}
```

**Frontend Usage:**
```typescript
// In records-list.component.ts
ngOnInit() {
  this.validationService.currentJob$
    .pipe(takeUntil(this.destroy$))
    .subscribe(job => {
      if (job) {
        this.currentJobStatus = job.status;
        this.jobProgress = (job.successfulRecords / job.totalRecords) * 100;
      }
    });
}
```

---

### 3. Get Latest Job
**Backend Method:** `ValidationController.GetLatestJobAsync()`
```csharp
GET /api/validation/latest
Response: { jobId, status, createdAt, ... }
```

**Frontend Service Call:**
```typescript
// In ValidationService
getLatestJob(): Observable<JobStatusResponse> {
  return this.apiService.get<JobStatusResponse>('/validation/latest');
}
```

**Frontend Usage:**
```typescript
// On component load - restore previous job state
ngOnInit() {
  this.validationService.getLatestJob().subscribe({
    next: (job) => {
      this.validationService.currentJob$.next(job);
      // Resume polling if still running
      if (job.status === 'Running') {
        this.validationService.pollJobStatus(job.jobId);
      }
    }
  });
}
```

---

### 4. Fetch All Records
**Backend Method:** `RecordsController.GetRecordsAsync()`
```csharp
GET /api/records
Response: Array of {
  InputPMID: string,
  MatchedPMID: string,
  ConfidenceScore: number,
  Match_Extent: string,  // "EXACT_MATCH" | "MINOR_CHANGE" | "MAJOR_DISCREPANCY" | "NO_MATCH"
  Discrepancies_Logical: string,
  Discrepancies_Metadata: string,
  Summary: string,
  OriginalTitle: string
}
Status: 200
Error: 404 if no records found, 500 if server error
```

**Frontend Service Call:**
```typescript
// In RecordService
getRecords(): Observable<ValidatedRecord[]> {
  return this.apiService.get<ValidatedRecord[]>('/records').pipe(
    tap(records => {
      this.records$.next(records);
      this.isLoading$.next(false);
    }),
    catchError(error => {
      this.error$.next('Failed to load records');
      this.isLoading$.next(false);
      return throwError(() => new Error(error.message));
    })
  );
}
```

**Frontend Usage:**
```typescript
// In records-list.component.ts - Load records on init
ngOnInit() {
  this.loadRecords();
}

loadRecords() {
  this.recordService.getRecords().subscribe({
    next: (records) => {
      this.allRecords = records;
      this.onFilterChange(); // Apply current filters
    },
    error: (err) => {
      this.error = 'Failed to load records from server';
    }
  });
}
```

---

### 5. Get Single Record by Index
**Backend Method:** `RecordsController.GetRecordAsync(index)`
```csharp
GET /api/records/{index}
Parameters:
  index: int (0-based array index)
Response: {
  InputPMID, MatchedPMID, ConfidenceScore, Match_Extent,
  Discrepancies_Logical, Discrepancies_Metadata, Summary, OriginalTitle
}
Status: 200
Error: 400 if index invalid, 404 if not found
```

**Frontend Service Call:**
```typescript
// In RecordService
getRecordByIndex(index: number): Observable<ValidatedRecord> {
  return this.apiService.get<ValidatedRecord>(`/records/${index}`);
}
```

**Frontend Usage:**
```typescript
// Not currently used - all records loaded at once
// Could be used for lazy-loading or detail-on-demand
getRecordDetails(index: number) {
  this.recordService.getRecordByIndex(index).subscribe({
    next: (record) => {
      this.selectedRecord = record;
    }
  });
}
```

---

## Data Models & Type Conversions

### ValidatedRecord (Frontend Interface)
```typescript
export interface ValidatedRecord {
  InputPMID: string;
  MatchedPMID: string;
  ConfidenceScore: number;        // 0.0 - 1.0
  Match_Extent: MatchExtent;      // Enum string
  Discrepancies_Logical: string;  // "None" or detailed list
  Discrepancies_Metadata: string; // "None" or detailed list
  Summary: string;                // Full description
  OriginalTitle: string;          // Full title text
}

export enum MatchExtent {
  EXACT_MATCH = 'EXACT_MATCH',
  MINOR_CHANGE = 'MINOR_CHANGE',
  MAJOR_DISCREPANCY = 'MAJOR_DISCREPANCY',
  NO_MATCH = 'NO_MATCH'
}
```

### Type Conversion in Services
```typescript
// In record.service.ts - Type safety conversion
getRecords(): Observable<ValidatedRecord[]> {
  return this.apiService.get<any[]>('/records').pipe(
    map(records => 
      records.map(record => ({
        ...record,
        Match_Extent: record.Match_Extent as MatchExtent,
        ConfidenceScore: parseFloat(record.ConfidenceScore.toString())
      }))
    )
  );
}
```

---

## Filter & Sort Operations (Client-Side)

### Filtering Logic
```typescript
// In record.service.ts - applyFilter() method
applyFilter(
  matchExtent?: MatchExtent,
  confidenceRange?: { min: number, max: number },
  searchTerm?: string,
  sortBy?: string,
  sortOrder?: 'asc' | 'desc'
): Observable<ValidatedRecord[]> {
  
  const filtered = this.records$.value.filter(record => {
    // Match type filter
    if (matchExtent && record.Match_Extent !== matchExtent) return false;
    
    // Confidence range filter
    if (confidenceRange) {
      if (record.ConfidenceScore < confidenceRange.min || 
          record.ConfidenceScore > confidenceRange.max) return false;
    }
    
    // Search filter (PMID + Title)
    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      if (!record.InputPMID.includes(term) && 
          !record.OriginalTitle.toLowerCase().includes(term)) {
        return false;
      }
    }
    
    return true;
  });
  
  // Sort
  if (sortBy) {
    filtered.sort((a, b) => {
      const aVal = a[sortBy as keyof ValidatedRecord];
      const bVal = b[sortBy as keyof ValidatedRecord];
      
      const comparison = aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
      return sortOrder === 'desc' ? -comparison : comparison;
    });
  }
  
  this.filteredRecords$.next(filtered);
  return of(filtered);
}
```

### Feature: Match Type Flagging
```typescript
// In records-list.component.ts
getMatchTypeInfo(matchExtent: MatchExtent) {
  return MATCH_TYPE_CONFIG[matchExtent];
}

// Template uses it:
// <span [style.backgroundColor]="getMatchTypeInfo(record.Match_Extent).bgColor">
//   {{ getMatchTypeInfo(record.Match_Extent).label }}
// </span>
```

---

## Error Handling

### Global Error Interceptor
```typescript
// In error.interceptor.ts
intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
  return next.handle(request).pipe(
    catchError((error: HttpErrorResponse) => {
      const apiError: ApiError = {
        status: error.status,
        message: this.getErrorMessage(error),
        error: error.error
      };
      
      // Status-based messages
      switch (error.status) {
        case 0:
          apiError.message = 'Unable to connect to backend API. Check if server is running.';
          break;
        case 400:
          apiError.message = 'Invalid request to server.';
          break;
        case 404:
          apiError.message = 'Requested resource not found.';
          break;
        case 500:
          apiError.message = 'Server error occurred.';
          break;
      }
      
      return throwError(() => apiError);
    })
  );
}
```

### Service Error Handling
```typescript
// In record.service.ts
getRecords(): Observable<ValidatedRecord[]> {
  return this.apiService.get<ValidatedRecord[]>('/records').pipe(
    catchError(error => {
      const errorMsg = error?.message || 'Failed to fetch records';
      this.error$.next(errorMsg);
      return throwError(() => new Error(errorMsg));
    })
  );
}
```

### Component Error Handling
```typescript
// In records-list.component.ts
loadRecords() {
  this.recordService.getRecords().subscribe({
    next: (records) => {
      this.allRecords = records;
    },
    error: (err) => {
      this.errorMessage = err.message;
      // Error banner displays automatically
    }
  });
}
```

---

## State Management Flow

### Data Flow Diagram
```
Backend API
    ↓
ApiService (HTTP wrapper)
    ↓
[ValidationService | RecordService]
    ↓
Components (Subscribe to Observables)
    ↓
Update UI
```

### Observable Chain Example
```typescript
// RecordService manages state with BehaviorSubjects
records$ = new BehaviorSubject<ValidatedRecord[]>([]);
filteredRecords$ = new BehaviorSubject<ValidatedRecord[]>([]);
isLoading$ = new BehaviorSubject<boolean>(false);
error$ = new BehaviorSubject<string | null>(null);

// Components subscribe
ngOnInit() {
  // Initial load
  this.recordService.getRecords().subscribe();
  
  // Subscribe to filtered results
  this.recordService.filteredRecords$
    .pipe(takeUntil(this.destroy$))
    .subscribe(records => {
      this.displayedRecords = this.paginate(records);
    });
}
```

---

## Common Integration Issues & Solutions

### Issue 1: CORS Error - No 'Access-Control-Allow-Origin' header
**Cause:** Backend CORS not configured
**Solution:** Ensure backend has:
```csharp
builder.Services.AddCors(options => {
  options.AddPolicy("AllowAngular", policy =>
    policy.WithOrigins("http://localhost:4200")
          .AllowAnyMethod()
          .AllowAnyHeader());
});
app.UseCors("AllowAngular");
```

### Issue 2: 404 on /api/records
**Cause:** Backend not serving records or wrong endpoint
**Solution:** Check:
- Backend is running on port 5000
- Endpoint is exactly `/api/records`
- Data is in results.json file

### Issue 3: "Match_Extent is undefined"
**Cause:** Backend returning different field name or type
**Solution:** Ensure enum conversion:
```typescript
Match_Extent: record.Match_Extent as MatchExtent
```

### Issue 4: Polling never stops
**Cause:** Status never reaches "Completed" or "Failed"
**Solution:** Debug by logging:
```typescript
tap(response => {
  console.log('Job Status:', response.status);
  if (['Completed', 'Failed'].includes(response.status)) {
    this.isPolling$.next(false);
  }
})
```

---

## Development Debugging Tips

### Enable Network Logging
```typescript
// In api.service.ts
get<T>(endpoint: string): Observable<T> {
  console.log(`[API] GET ${this.apiBaseUrl}${endpoint}`);
  return this.http.get<T>(`${this.apiBaseUrl}${endpoint}`).pipe(
    tap(response => console.log(`[API Response]`, response)),
    catchError(this.handleError)
  );
}
```

### Test API Endpoints in Browser
```javascript
// In browser console
fetch('http://localhost:5000/api/records')
  .then(r => r.json())
  .then(data => console.log(data));
```

### Check Service State
```typescript
// In any component
constructor(private recordService: RecordService) {}

debugState() {
  this.recordService.records$.subscribe(r => console.log('Records:', r));
  this.recordService.filteredRecords$.subscribe(f => console.log('Filtered:', f));
}
```

---

## Performance Considerations

### Current Architecture
- **Data Loading:** All records loaded once on app init (good for < 1000 records)
- **Filtering:** Client-side (instant, but memory-intensive for large datasets)
- **Polling:** 2-second interval (reasonable, could be optimized with WebSocket)

### Optimization Strategies
1. **Backend Pagination:** Implement GET /api/records?page=1&pageSize=20
2. **Lazy Loading:** Load records on-demand with virtual scrolling
3. **Server Push:** Replace polling with WebSocket for real-time updates
4. **Caching:** Add NgRx/Akita for complex state management

---

## Testing Integration

### API Mock for Testing
```typescript
// Create mock service
@Injectable()
class MockApiService {
  get<T>(endpoint: string): Observable<T> {
    if (endpoint === '/records') {
      return of(MOCK_RECORDS as any);
    }
    return throwError(() => new Error('Not mocked'));
  }
}

// Use in test
TestBed.configureTestingModule({
  providers: [
    { provide: ApiService, useClass: MockApiService }
  ]
});
```

---

**Document Version:** 1.0
**Last Updated:** 2024
**Backend Version:** ASP.NET Core 6.0+
**Frontend Version:** Angular 17+
