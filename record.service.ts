import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { ValidatedRecord, MatchExtent } from '../../shared/models/validated-record';

/**
 * Service for managing validated records
 */
@Injectable({
  providedIn: 'root'
})
export class RecordService {
  private recordsSubject = new BehaviorSubject<ValidatedRecord[]>([]);
  public records$ = this.recordsSubject.asObservable();

  private filteredRecordsSubject = new BehaviorSubject<ValidatedRecord[]>([]);
  public filteredRecords$ = this.filteredRecordsSubject.asObservable();

  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  public isLoading$ = this.isLoadingSubject.asObservable();

  private errorSubject = new BehaviorSubject<string | null>(null);
  public error$ = this.errorSubject.asObservable();

  constructor(private apiService: ApiService) {}

  /**
   * Get all validated records
   */
  getRecords(): Observable<ValidatedRecord[]> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);
    
    console.log('[DEBUG - RecordService] Fetching records from backend API: GET /records');

    return this.apiService.get<ValidatedRecord[]>('/records').pipe(
      tap({
        next: (records) => {
          console.log(`[DEBUG - RecordService] Successfully received ${records ? records.length : 0} records from backend.`);
          
          if (!records || records.length === 0) {
            console.warn('[DEBUG - RecordService] The backend returned an empty array or null. This means the JSON file is likely empty or no validation job has produced results yet.');
          } else {
            console.log('[DEBUG - RecordService] Sample of first record received:', records[0]);
          }

          // Convert match_Extent string to enum if needed
          const processedRecords = records.map(record => ({
            ...record,
            match_Extent: record.match_Extent as MatchExtent
          }));
          
          this.recordsSubject.next(processedRecords);
          this.filteredRecordsSubject.next(processedRecords);
          this.isLoadingSubject.next(false);
          
          console.log(`[DEBUG - RecordService] Data processing complete. Emitted ${processedRecords.length} records to subscribers.`);
        },
        error: (err) => {
          console.error('[DEBUG - RecordService] Error occurred while loading records:', err);
          this.errorSubject.next(err.message || 'Failed to load records');
          this.isLoadingSubject.next(false);
        }
      })
    );
  }

  /**
   * Get validated records for a specific job
   */
  getRecordsForJob(jobId: string): Observable<ValidatedRecord[]> {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);
    
    console.log(`[DEBUG - RecordService] Fetching records for job ${jobId} from backend API: GET /records/job/${jobId}`);

    return this.apiService.get<ValidatedRecord[]>(`/records/job/${jobId}`).pipe(
      tap({
        next: (records) => {
          console.log(`[DEBUG - RecordService] Successfully received ${records ? records.length : 0} records for job ${jobId}.`);
          
          const processedRecords = records.map(record => ({
            ...record,
            match_Extent: record.match_Extent as MatchExtent
          }));
          
          this.recordsSubject.next(processedRecords);
          this.filteredRecordsSubject.next(processedRecords);
          this.isLoadingSubject.next(false);
        },
        error: (err) => {
          console.error(`[DEBUG - RecordService] Error occurred while loading records for job ${jobId}:`, err);
          this.errorSubject.next(err.message || `Failed to load records for job ${jobId}. Data might be missing.`);
          this.isLoadingSubject.next(false);
          // Set empty state on error so UI can show fallback message
          this.recordsSubject.next([]);
          this.filteredRecordsSubject.next([]);
        }
      })
    );
  }

  /**
   * Get a single record by index
   */
  getRecord(index: number): Observable<ValidatedRecord> {
    return this.apiService.get<ValidatedRecord>(`/records/${index}`);
  }

  /**
   * Refresh records
   */
  refreshRecords(): void {
    this.getRecords().subscribe();
  }

  /**
   * Apply filters and sorting to records
   */
  applyFilter(
    matchExtent?: MatchExtent | null,
    confidenceScoreRange?: [number, number],
    searchTerm?: string,
    sortBy?: string,
    sortOrder?: 'asc' | 'desc'
  ): void {
    const records = this.recordsSubject.value;
    
    let filtered = [...records];

    // Filter by match extent
    if (matchExtent) {
      filtered = filtered.filter(r => r.match_Extent === matchExtent);
    }

    // Filter by confidence score range
    if (confidenceScoreRange) {
      const [min, max] = confidenceScoreRange;
      filtered = filtered.filter(r => r.confidenceScore >= min && r.confidenceScore <= max);
    }

    // Search by PMID or title
    if (searchTerm && searchTerm.trim()) {
      const term = searchTerm.toLowerCase();
      filtered = filtered.filter(r =>
        r.inputPMID.toLowerCase().includes(term) ||
        r.matchedPMID.toLowerCase().includes(term) ||
        r.originalTitle.toLowerCase().includes(term)
      );
    }

    // Sort
    if (sortBy) {
      filtered.sort((a, b) => {
        let valueA: any = (a as any)[sortBy];
        let valueB: any = (b as any)[sortBy];

        if (typeof valueA === 'string') {
          valueA = valueA.toLowerCase();
          valueB = (valueB as string).toLowerCase();
        }

        const comparison = valueA < valueB ? -1 : valueA > valueB ? 1 : 0;
        return sortOrder === 'desc' ? -comparison : comparison;
      });
    }

    this.filteredRecordsSubject.next(filtered);
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.filteredRecordsSubject.next(this.recordsSubject.value);
  }

  /**
   * Get current records
   */
  getCurrentRecords(): ValidatedRecord[] {
    return this.recordsSubject.value;
  }

  /**
   * Get current filtered records
   */
  getCurrentFilteredRecords(): ValidatedRecord[] {
    return this.filteredRecordsSubject.value;
  }

  /**
   * Get records count
   */
  getRecordsCount(): number {
    return this.recordsSubject.value.length;
  }

  /**
   * Get filtered records count
   */
  getFilteredRecordsCount(): number {
    return this.filteredRecordsSubject.value.length;
  }

  /**
   * Clear error message
   */
  clearError(): void {
    this.errorSubject.next(null);
  }
}
