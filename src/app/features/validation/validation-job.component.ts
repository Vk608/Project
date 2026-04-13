import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ValidationService } from '../../core/services/validation.service';
import { RecordService } from '../../core/services/record.service';
import { BatchJob, JobStatus } from '../../shared/models/batch-job';
import { ErrorBannerComponent } from '../../shared/components/error-banner.component';

@Component({
  selector: 'app-validation-job',
  standalone: true,
  imports: [CommonModule, FormsModule, ErrorBannerComponent],
  templateUrl: './validation-job.component.html',
  styleUrls: ['./validation-job.component.scss']
})
export class ValidationJobComponent implements OnInit, OnDestroy {
  currentJob: BatchJob | null = null;
  isPolling: boolean = false;
  isStarting: boolean = false;
  error: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private validationService: ValidationService,
    private recordService: RecordService
  ) {}

  ngOnInit(): void {
    this.validationService.currentJob$.pipe(takeUntil(this.destroy$)).subscribe(job => {
      this.currentJob = job;
    });

    this.validationService.isPolling$.pipe(takeUntil(this.destroy$)).subscribe(isPolling => {
      this.isPolling = isPolling;
    });

    // Try to load latest job on init
    this.validationService.getLatestJob().subscribe({
      error: () => {
        // No jobs yet - that's ok
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  startValidation(): void {
    this.isStarting = true;
    this.error = null;

    this.validationService.startValidation().subscribe({
      next: (job) => {
        this.currentJob = job;
        this.isStarting = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to start validation';
        this.isStarting = false;
      }
    });
  }

  stopPolling(): void {
    this.validationService.stopPolling();
  }

  getProgressPercentage(): number {
    if (!this.currentJob || this.currentJob.totalRecords === 0) {
      return 0;
    }
    return (this.currentJob.successfulRecords / this.currentJob.totalRecords) * 100;
  }

  getJobStatusColor(): string {
    if (!this.currentJob) return '';
    
    const status = this.currentJob.status;
    if (status === JobStatus.PENDING) return '#ff9800';
    if (status === JobStatus.RUNNING) return '#2196f3';
    if (status === JobStatus.COMPLETED) return '#4caf50';
    if (status === JobStatus.FAILED) return '#f44336';
    return '';
  }

  onErrorClosed(): void {
    this.error = null;
  }
}
