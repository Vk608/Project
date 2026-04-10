import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { RecordService } from '../../core/services/record.service';
import { ValidationService } from '../../core/services/validation.service';
import { ValidatedRecord, MatchExtent, MATCH_TYPE_CONFIG } from '../../shared/models/validated-record';
import { ValidationJobComponent } from '../validation/validation-job.component';
import { ErrorBannerComponent } from '../../shared/components/error-banner.component';

@Component({
  selector: 'app-records-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ValidationJobComponent, ErrorBannerComponent],
  templateUrl: './records-list.component.html',
  styleUrls: ['./records-list.component.scss']
})
export class RecordsListComponent implements OnInit, OnDestroy {
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

  constructor(
    private recordService: RecordService,
    private validationService: ValidationService
  ) {}

  ngOnInit(): void {
    this.loadRecords();

    this.recordService.filteredRecords$.pipe(takeUntil(this.destroy$)).subscribe(records => {
      this.records = records;
      this.currentPage = 1; // Reset to first page on filter change
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
        }, 1000);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRecords(): void {
    this.recordService.getRecords().subscribe();
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

  getMatchTypeInfo(matchExtent: MatchExtent) {
    return MATCH_TYPE_CONFIG[matchExtent];
  }

  getConfidenceClass(score: number): string {
    if (score >= 0.8) return 'confidence-high';
    if (score >= 0.6) return 'confidence-medium';
    return 'confidence-low';
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

  getMatchBadgeStyles(matchExtent: MatchExtent): any {
    const info = MATCH_TYPE_CONFIG[matchExtent];
    return {
      'color': info.color,
      'backgroundColor': info.bgColor
    };
  }
}
