# Q2: Frontend Filters & Search - Implementation Logic

## High-Level Overview

The FAB Batch Validator provides **client-side filtering and searching** on the records list. All filtering happens **in the browser** (frontend), not on the backend.

---

## Architecture: Client-Side Filtering

### **Design Pattern:**
```
Raw Records (from Backend)
        ↓
[RecordService - applyFilter()]
        ↓
Filter & Search Operations
        ↓
Filtered Records (stored in BehaviorSubject)
        ↓
UI Updates with Filtered Results
```

### **Why Client-Side?**
- All records already loaded into memory on frontend
- No additional HTTP requests needed per filter change
- **Instant** user experience (no server latency)
- Reduces backend load

---

## Components Involved

### **1. RecordService** (`Frontend/src/app/core/services/record.service.ts`)
Manages record data and filtering logic

### **2. RecordsListComponent** (`Frontend/src/app/features/records/records-list.component.ts`)
UI component that displays records and provides filter controls

---

## Filter Types & Implementation

### **Filter #1: Match Type (Match Extent)**

**User Selection:** Dropdown with options:
- Exact
- Partial
- No Match
- (or None = no filter)

**Implementation Logic:**
```typescript
// In RecordService.applyFilter()
if (matchExtent) {
    filtered = filtered.filter(r => r.match_Extent === matchExtent);
    // E.g., only keep records where match_Extent === "Exact"
}
```

**Data Model:**
```typescript
enum MatchExtent {
  Exact = "Exact",
  Partial = "Partial",
  NoMatch = "No Match"
}

// Configuration for display
const MATCH_TYPE_CONFIG = {
  Exact: { label: "Exact Match", color: "#4caf50", icon: "check" },
  Partial: { label: "Partial Match", color: "#ff9800", icon: "partial" },
  NoMatch: { label: "No Match", color: "#f44336", icon: "close" }
};
```

**UI Control:**
```html
<select [(ngModel)]="filterMatchExtent" (change)="onFilterChange()">
  <option [value]="null">All Match Types</option>
  <option value="Exact">Exact Match</option>
  <option value="Partial">Partial Match</option>
  <option value="No Match">No Match</option>
</select>
```

---

### **Filter #2: Confidence Score Range**

**User Selection:** Two Number Inputs
- Min: 0 to 1
- Max: 0 to 1

**Implementation Logic:**
```typescript
// In RecordService.applyFilter()
if (confidenceScoreRange) {
    const [min, max] = confidenceScoreRange;
    filtered = filtered.filter(r => 
        r.confidenceScore >= min && r.confidenceScore <= max
    );
    // E.g., only keep records where score is between 0.7 and 1.0
}
```

**UI Control:**
```html
<input type="number" min="0" max="1" step="0.1" 
       [(ngModel)]="filterConfidenceMin" (change)="onFilterChange()" />
Min Score: {{ filterConfidenceMin }}

<input type="number" min="0" max="1" step="0.1" 
       [(ngModel)]="filterConfidenceMax" (change)="onFilterChange()" />
Max Score: {{ filterConfidenceMax }}
```

---

### **Filter #3: Free Text Search**

**User Selection:** Single Text Input
- Can search by: PMID, Title

**Implementation Logic:**
```typescript
// In RecordService.applyFilter()
if (searchTerm && searchTerm.trim()) {
    const term = searchTerm.toLowerCase();
    filtered = filtered.filter(r =>
        r.inputPMID.toLowerCase().includes(term) ||      // Search input PMID
        r.matchedPMID.toLowerCase().includes(term) ||    // Search matched PMID
        r.originalTitle.toLowerCase().includes(term)     // Search title
    );
}
```

**Search Behavior:**
- **Case-insensitive** (converts to lowercase)
- **Substring matching** (PMID "12345" matches input "234")
- **Multi-field** (searches PMID + Title at once)

**UI Control:**
```html
<input type="text" 
       [(ngModel)]="searchTerm" 
       placeholder="Search PMID or title..." 
       (keyup)="onSearch()" />
```

**Example Searches:**
```
User types "PMID:123"  → Shows records with PMID containing "123"
User types "biblio"    → Shows records with title containing "biblio"
User types "EXACT pmid" → Shows records matching CASE-INSENSITIVE search
```

---

### **Filter #4: Sorting**

**User Selection:** Click column header to sort

**Implementation Logic:**
```typescript
onSort(column: string): void {
    if (this.sortBy === column) {
        // Already sorting by this column → toggle direction
        this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
        // New column → default to ascending
        this.sortBy = column;
        this.sortOrder = 'asc';
    }
    this.onFilterChange();  // Re-apply filters with new sort
}

// Inside applyFilter():
if (sortBy) {
    filtered.sort((a, b) => {
        let valueA: any = (a as any)[sortBy];      // Get column value from record
        let valueB: any = (b as any)[sortBy];

        // Handle strings (case-insensitive)
        if (typeof valueA === 'string') {
            valueA = valueA.toLowerCase();
            valueB = (valueB as string).toLowerCase();
        }

        // Compare
        const comparison = valueA < valueB ? -1 : valueA > valueB ? 1 : 0;
        
        // Apply sort direction
        return sortOrder === 'desc' ? -comparison : comparison;
    });
}
```

**Supported Sort Columns:**
- PMID
- Match Type
- Confidence Score
- Title
- (others from ValidatedRecord model)

**UI Behavior:**
```html
<th (click)="onSort('inputPMID')">
  PMID
  <span *ngIf="sortBy === 'inputPMID'">
    {{ sortOrder === 'asc' ? '↑' : '↓' }}
  </span>
</th>
```

---

## Filter Flow: Step-by-Step Execution

### **Scenario: User applies all filters at once**

**Initial State:**
- User loads page
- All records loaded from backend
- Stored in `recordsSubject` (in-memory)

**Step 1: User selects "Exact" and sets score 0.8-1.0**
```typescript
filterMatchExtent = "Exact";
filterConfidenceMin = 0.8;
filterConfidenceMax = 1.0;
// User types search term: "cancer"
searchTerm = "cancer";
```

**Step 2: User clicks "Apply Filters" or changes input**
```typescript
onFilterChange(): void {
    this.recordService.applyFilter(
        this.filterMatchExtent,                    // "Exact"
        [this.filterConfidenceMin, this.filterConfidenceMax],  // [0.8, 1.0]
        this.searchTerm || undefined,              // "cancer"
        this.sortBy || undefined,                  // "inputPMID"
        this.sortOrder                             // "asc"
    );
}
```

**Step 3: RecordService executes filter logic**
```typescript
applyFilter(
    matchExtent = "Exact",
    confidenceScoreRange = [0.8, 1.0],
    searchTerm = "cancer",
    sortBy = "inputPMID",
    sortOrder = "asc"
): void {
    let filtered = [...this.recordsSubject.value];  // Copy all records
    
    // Filter by match extent
    filtered = filtered.filter(r => r.match_Extent === "Exact");
    // Result: ~150 records (if 50% are exact matches)
    
    // Filter by confidence score
    const [min, max] = [0.8, 1.0];
    filtered = filtered.filter(r => r.confidenceScore >= 0.8 && r.confidenceScore <= 1.0);
    // Result: ~100 records (if 67% have high confidence)
    
    // Filter by search term
    filtered = filtered.filter(r =>
        r.inputPMID.toLowerCase().includes("cancer") ||
        r.matchedPMID.toLowerCase().includes("cancer") ||
        r.originalTitle.toLowerCase().includes("cancer")
    );
    // Result: ~45 records (if ~45% have "cancer" in PMID/title)
    
    // Sort
    filtered.sort((a, b) => {
        let valueA = a.inputPMID.toLowerCase();
        let valueB = b.inputPMID.toLowerCase();
        return valueA < valueB ? -1 : valueA > valueB ? 1 : 0;
    });
    // Result: 45 records, sorted by PMID ascending
    
    // Update UI
    this.filteredRecordsSubject.next(filtered);  // Emit to UI
}
```

**Step 4: UI updates with filtered records**
```typescript
// In RecordsListComponent
this.recordService.filteredRecords$.subscribe(records => {
    this.records = records;  // Update local array
    this.currentPage = 1;    // Reset pagination to page 1
    // Angular detects change → re-renders table
});

// HTML renders:
// <tr *ngFor="let record of records">...</tr>
// Only 45 rows display (filtered results)
```

---

## Performance Optimization

### **Issue: What if we have 100,000 records?**

**Current Implementation:**
```typescript
// Creates a NEW array, then filters it
let filtered = [...this.recordsSubject.value];  // O(n) copy
filtered = filtered.filter(...);                // O(n) filter
filtered.sort(...);                             // O(n log n) sort
```

**Time Complexity:**
- Copy: O(n)
- Each filter: O(n)
- Sort: O(n log n)
- **Total: O(n log n)**

For 100,000 records: ~1.6 million operations (still < 100ms typically)

### **Current Optimization:**
- Records stored in **Browser Memory** (not fetched per filter)
- Filters applied **locally** (no HTTP latency)
- Results update **instantly**

### **If Performance Becomes an Issue:**
- Implement **virtual scrolling** (render only visible rows)
- Add **debouncing** to search input (don't filter on every keystroke)
- Implement **lazy loading** for large datasets

---

## Pagination (Bonus)

**Not fully visible in filters, but mentioned in component:**

```typescript
// Component state
pageSize: number = 10;
currentPage: number = 1;

// Display only current page of filtered results
getPaginatedRecords(): ValidatedRecord[] {
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.records.slice(start, end);
}
```

**UI:**
```html
<tr *ngFor="let record of getPaginatedRecords()">...</tr>
<button (click)="currentPage = currentPage - 1">Previous</button>
<span>Page {{ currentPage }} of {{ getTotalPages() }}</span>
<button (click)="currentPage = currentPage + 1">Next</button>
```

---

## Clear Filters Action

**User clicks "Clear Filters" button:**

```typescript
clearFilters(): void {
    this.filterMatchExtent = null;
    this.filterConfidenceMin = 0;
    this.filterConfidenceMax = 1;
    this.searchTerm = '';
    this.sortBy = '';
    this.sortOrder = 'asc';
    
    // Reset filtered records to all records
    this.recordService.clearFilters();
    // Which calls:
    // this.filteredRecordsSubject.next(this.recordsSubject.value);
}
```

---

## Data Flow Diagram: Filtering

```
┌─────────────────────────────────────────────┐
│    UI Component (RecordsListComponent)       │
│  ┌───────────────────────────────────────┐  │
│  │ User Input:                           │  │
│  │ - filterMatchExtent: "Exact"          │  │
│  │ - filterConfidenceMin: 0.8            │  │
│  │ - filterConfidenceMax: 1.0            │  │
│  │ - searchTerm: "cancer"                │  │
│  │ - sortBy: "inputPMID"                 │  │
│  └───────────────────┬───────────────────┘  │
└─────────────────────┼──────────────────────┘
                      │
                      │ calls onFilterChange()
                      ↓
┌─────────────────────────────────────────────┐
│    RecordService (applyFilter method)        │
│  ┌───────────────────────────────────────┐  │
│  │ 1. Start: filtered = all records      │  │
│  │    (1000 records)                     │  │
│  └───────────────────┬───────────────────┘  │
│  ┌───────────────────↓───────────────────┐  │
│  │ 2. Filter by Match Extent             │  │
│  │    if (match_Extent === "Exact")      │  │
│  │    (500 records remain)                │  │
│  └───────────────────┬───────────────────┘  │
│  ┌───────────────────↓───────────────────┐  │
│  │ 3. Filter by Confidence Score         │  │
│  │    if (score >= 0.8 && score <= 1.0)  │  │
│  │    (333 records remain)                │  │
│  └───────────────────┬───────────────────┘  │
│  ┌───────────────────↓───────────────────┐  │
│  │ 4. Filter by Search Term              │  │
│  │    if (title.includes("cancer"))      │  │
│  │    (45 records remain)                 │  │
│  └───────────────────┬───────────────────┘  │
│  ┌───────────────────↓───────────────────┐  │
│  │ 5. Sort by Column                     │  │
│  │    sort(inputPMID, asc)               │  │
│  │    (45 records, sorted)                │  │
│  └───────────────────┬───────────────────┘  │
│  ┌───────────────────↓───────────────────┐  │
│  │ 6. Emit filtered results              │  │
│  │    filteredRecordsSubject.next(...)   │  │
│  └───────────────────┬───────────────────┘  │
└─────────────────────┼──────────────────────┘
                      │
                      │ BehaviorSubject emits
                      ↓
┌─────────────────────────────────────────────┐
│    UI Component (Subscriber)                │
│  ┌───────────────────────────────────────┐  │
│  │ records$ subscription updates:        │  │
│  │ this.records = [45 filtered items]    │  │
│  │                                       │  │
│  │ Angular re-renders table:             │  │
│  │ <tr *ngFor="let r of records">        │  │
│  │   (only 45 rows display)              │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

---

## Reactive Pattern: RxJS Observables

**The system uses RxJS (Reactive Extensions) for state management:**

```typescript
// RecordService publishes two streams:
public records$: Observable<ValidatedRecord[]>;           // ALL records
public filteredRecords$: Observable<ValidatedRecord[]>;  // Filtered records

// RecordsListComponent subscribes:
this.recordService.filteredRecords$.pipe(
    takeUntil(this.destroy$)  // Auto-unsubscribe on component destroy
).subscribe(records => {
    this.records = records;  // Update UI
});
```

**Benefits:**
- **Reactive**: UI updates automatically when data changes
- **Unsubscribe**: Component cleanup prevents memory leaks
- **Type-safe**: TypeScript knows exact structure

---

## Summary Table: Filter Operations

| Filter Type | User Input | Data Location | Filter Logic | Performance |
|------------|-----------|----------------|--------------|-------------|
| Match Type | Dropdown | `record.match_Extent` | Exact equality | O(n) |
| Confidence | Number inputs (2) | `record.confidenceScore` | Range comparison | O(n) |
| Search | Text input | 3 fields (PMID, Title) | Substring, case-insensitive | O(n * m) |
| Sort | Column click | Any record property | Comparison + array sort | O(n log n) |

**Legend:**
- `n` = number of records
- `m` = length of search term

---

## Key Takeaways

1. **Client-side filtering** = instant response, no server calls
2. **Reactive pattern** = automatic UI updates
3. **Progressive filtering** = apply filters one by one to reduce data
4. **Pagination** = handle large datasets efficiently
5. **User experience** = real-time feedback as users adjust filters
