import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, combineLatest } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ActivatedRoute } from '@angular/router';
import { RecordService } from '../../../core/services/record.service';
import { ValidationService } from '../../../core/services/validation.service';
import { ValidatedRecord, MatchExtent, MATCH_TYPE_CONFIG } from '../../../shared/models/validated-record';
import { ErrorBannerComponent } from '../../../shared/components/error-banner.component';
import { LoaderComponent } from '../../../shared/components/loader/loader.component';
import { CardComponent } from '../../../shared/components/card/card.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [CommonModule, FormsModule, ErrorBannerComponent, LoaderComponent, CardComponent, ButtonComponent],
  templateUrl: './results.component.html',
  styleUrls: ['./results.component.scss']
})
export class ResultsComponent implements OnInit, OnDestroy {
  records: ValidatedRecord[] = [];
  isLoading: boolean = false;
  error: string | null = null;

  // Filter state
  filterMatchExtent: MatchExtent | null = null;
  filterConfidenceMin: number = 0;
  filterConfidenceMax: number = 1;
  searchTerm: string = '';

  // Sorting state
  sortBy: string = '';
  sortOrder: 'asc' | 'desc' = 'asc';

  // Match type options
  matchTypeOptions = Object.values(MatchExtent);
  matchTypeConfig = MATCH_TYPE_CONFIG;

  // Pagination
  pageSize: number = 10;
  currentPage: number = 1;

  // Expanded records tracking
  expandedRecords: { [key: string]: boolean } = {};

  // Math reference for template
  Math = Math;

  private destroy$ = new Subject<void>();
  jobId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private recordService: RecordService,
    private validationService: ValidationService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    // Subscribe to route parameters to detect jobId
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.jobId = params.get('jobId');
      this.loadRecords();
    });

    this.recordService.filteredRecords$.pipe(takeUntil(this.destroy$)).subscribe(records => {
      console.log(`[DEBUG - ResultsComponent] Received new dataset from RecordService. Total records: ${records ? records.length : 0}`);
      this.records = records;
      
      if (records && records.length > 0) {
        console.log(`[DEBUG - ResultsComponent] Ready to render data. Sample record:`, records[0]);
      } else {
        console.warn(`[DEBUG - ResultsComponent] Data array is empty. The empty state ("No validated records found") will be displayed.`);
      }
      
      this.currentPage = 1; // Reset to first page on filter change
      console.log(`[DEBUG - ResultsComponent] Setting current page to 1. Expected paginated items: ${Math.min(this.pageSize, records.length)}`);
    });

    this.recordService.isLoading$.pipe(takeUntil(this.destroy$)).subscribe(isLoading => {
      this.isLoading = isLoading;
    });

    this.recordService.error$.pipe(takeUntil(this.destroy$)).subscribe(error => {
      this.error = error;
    });

    // Auto-refresh records when validation completes
    this.validationService.currentJob$.pipe(takeUntil(this.destroy$)).subscribe(job => {
      if (job && job.status === 'Completed') {
        setTimeout(() => {
          this.recordService.refreshRecords();
          this.toastService.show('Results refreshed from latest validation.', 'success');
        }, 1000);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRecords(): void {
    if (this.jobId) {
      this.recordService.getRecordsForJob(this.jobId).subscribe();
    } else {
      this.recordService.getRecords().subscribe();
    }
  }

  onFilterChange(): void {
    this.recordService.applyFilter(
      this.filterMatchExtent,
      [this.filterConfidenceMin, this.filterConfidenceMax],
      this.searchTerm || undefined,
      this.sortBy || undefined,
      this.sortOrder
    );
  }

  onSearch(): void {
    this.onFilterChange();
  }

  onSort(column: string): void {
    if (this.sortBy === column) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortOrder = 'asc';
    }
    this.onFilterChange();
  }

  clearFilters(): void {
    this.filterMatchExtent = null;
    this.filterConfidenceMin = 0;
    this.filterConfidenceMax = 1;
    this.searchTerm = '';
    this.sortBy = '';
    this.sortOrder = 'asc';
    this.recordService.clearFilters();
  }

  getMatchTypeInfo(matchExtent: MatchExtent | string) {
    if (!matchExtent) {
      return { label: 'Unknown', cssClass: 'match-none', icon: 'help' };
    }
    
    // Normalize string to match enum format (e.g. "Exact match" -> "EXACT_MATCH")
    const normalizedKey = String(matchExtent).toUpperCase().replace(/ /g, '_');
    
    // Check if the normalized key exists in the config
    const config = MATCH_TYPE_CONFIG[normalizedKey as MatchExtent];
    
    if (config) {
      return config;
    }
    
    // Fallback for completely unrecognized values
    return { 
      label: String(matchExtent), 
      cssClass: 'match-none',
      icon: 'help'
    };
  }

  getSortIndicator(column: string): string {
    if (this.sortBy !== column) return '';
    return this.sortOrder === 'asc' ? '▲' : '▼';
  }

  get paginatedRecords(): ValidatedRecord[] {
    const startIdx = (this.currentPage - 1) * this.pageSize;
    return this.records.slice(startIdx, startIdx + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.records.length / this.pageSize);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  onErrorClosed(): void {
    this.recordService.clearError();
  }

  toggleDetails(pmid: string): void {
    this.expandedRecords[pmid] = !this.expandedRecords[pmid];
  }

  getEndIndex(): number {
    return Math.min(this.currentPage * this.pageSize, this.records.length);
  }
}
