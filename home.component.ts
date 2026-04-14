import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ValidationService } from '../../../core/services/validation.service';
import { BatchJob, JobStatus } from '../../../shared/models/batch-job';
import { ErrorBannerComponent } from '../../../shared/components/error-banner.component';
import { LoaderComponent } from '../../../shared/components/loader/loader.component';
import { CardComponent } from '../../../shared/components/card/card.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, ErrorBannerComponent, LoaderComponent, CardComponent, ButtonComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  currentJob: BatchJob | null = null;
  isPolling: boolean = false;
  isStarting: boolean = false;
  error: string | null = null;
  selectedFile: File | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private validationService: ValidationService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.validationService.currentJob$.pipe(takeUntil(this.destroy$)).subscribe(job => {
      if (job && job.status === 'Completed' && this.currentJob?.status !== 'Completed') {
        this.toastService.show('Validation completed successfully!', 'success');
      }
      if (job && job.status === 'Failed' && this.currentJob?.status !== 'Failed') {
        this.toastService.show('Validation job failed. Check error details.', 'error');
      }
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
    if (!this.selectedFile) {
      this.error = 'Please select a valid Excel file first.';
      return;
    }

    this.isStarting = true;
    this.error = null;

    this.validationService.startValidation(this.selectedFile).subscribe({
      next: (job) => {
        this.currentJob = job;
        this.isStarting = false;
        this.toastService.show('Validation job started.', 'info');
      },
      error: (err) => {
        this.error = err.message || 'Failed to start validation';
        this.isStarting = false;
        this.toastService.show(this.error!, 'error');
      }
    });
  }

  stopPolling(): void {
    this.validationService.stopPolling();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      this.error = null; // Clear any previous selection errors
    } else {
      this.selectedFile = null;
    }
  }

  getProgressPercentage(): number {
    if (!this.currentJob || this.currentJob.totalRecords === 0) {
      return 0;
    }
    return (this.currentJob.successfulRecords / this.currentJob.totalRecords) * 100;
  }

  getStatusBadgeClass(): string {
    if (!this.currentJob) return '';
    const status = this.currentJob.status;
    if (status === JobStatus.PENDING)   return 'badge-pending';
    if (status === JobStatus.RUNNING)   return 'badge-running';
    if (status === JobStatus.COMPLETED) return 'badge-completed';
    if (status === JobStatus.FAILED)    return 'badge-failed';
    return '';
  }

  onErrorClosed(): void {
    this.error = null;
  }
}
