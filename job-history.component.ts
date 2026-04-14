import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Router } from '@angular/router';
import { ValidationService } from '../../../core/services/validation.service';
import { BatchJob, JobStatus } from '../../../shared/models/batch-job';
import { ErrorBannerComponent } from '../../../shared/components/error-banner.component';
import { LoaderComponent } from '../../../shared/components/loader/loader.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-job-history',
  standalone: true,
  imports: [CommonModule, ErrorBannerComponent, LoaderComponent, ButtonComponent],
  templateUrl: './job-history.component.html',
  styleUrls: ['./job-history.component.scss']
})
export class JobHistoryComponent implements OnInit, OnDestroy {
  jobs: BatchJob[] = [];
  isLoading: boolean = false;
  error: string | null = null;
  currentJob: BatchJob | null = null;

  // Pagination
  pageSize: number = 10;
  currentPage: number = 1;

  // Math reference for template
  Math = Math;

  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private validationService: ValidationService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadJobHistory();

    // Subscribe to current job changes
    this.validationService.currentJob$.pipe(takeUntil(this.destroy$)).subscribe(job => {
      this.currentJob = job;
      // Reload history if a new job completes
      if (job && job.status === 'Completed') {
        setTimeout(() => {
          this.loadJobHistory();
        }, 1000);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadJobHistory(): void {
    this.isLoading = true;
    this.error = null;

    // Fetch all jobs (or use a dedicated endpoint if available)
    this.validationService.getAllJobs().subscribe({
      next: (jobs) => {
        this.jobs = jobs.sort((a: BatchJob, b: BatchJob) =>
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
        this.isLoading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load job history';
        this.isLoading = false;
        this.toastService.show(this.error!, 'error');
      }
    });
  }

  get paginatedJobs(): BatchJob[] {
    const startIdx = (this.currentPage - 1) * this.pageSize;
    return this.jobs.slice(startIdx, startIdx + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.jobs.length / this.pageSize);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  getStatusBadgeClass(status: string): string {
    if (status === JobStatus.PENDING)   return 'badge-pending';
    if (status === JobStatus.RUNNING)   return 'badge-running';
    if (status === JobStatus.COMPLETED) return 'badge-completed';
    if (status === JobStatus.FAILED)    return 'badge-failed';
    return '';
  }

  getProgressPercentage(job: BatchJob): number {
    if (job.totalRecords === 0) return 0;
    return (job.successfulRecords / job.totalRecords) * 100;
  }

  getSuccessRate(job: BatchJob): string {
    if (job.totalRecords === 0) return '0%';
    const rate = (job.successfulRecords / job.totalRecords) * 100;
    return rate.toFixed(1) + '%';
  }

  onErrorClosed(): void {
    this.error = null;
  }

  isCurrentJob(job: BatchJob): boolean {
    return this.currentJob !== null && this.currentJob.jobId === job.jobId;
  }

  viewResults(jobId: string): void {
    this.router.navigate(['/results', jobId]);
  }
}
