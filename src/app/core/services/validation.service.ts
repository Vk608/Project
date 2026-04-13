import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, interval, Subject } from 'rxjs';
import { switchMap, takeUntil, tap, map } from 'rxjs/operators';
import { ApiService } from './api.service';
import { BatchJob, ValidateResponse, JobStatusResponse } from '../../shared/models/batch-job';

/**
 * Service for managing validation jobs
 */
@Injectable({
  providedIn: 'root'
})
export class ValidationService {
  private currentJobSubject = new BehaviorSubject<BatchJob | null>(null);
  public currentJob$ = this.currentJobSubject.asObservable();

  private isPollingSubject = new BehaviorSubject<boolean>(false);
  public isPolling$ = this.isPollingSubject.asObservable();

  private pollStopSubject = new Subject<void>();

  constructor(private apiService: ApiService) {}

  /**
   * Start a new validation job
   */
  startValidation(): Observable<BatchJob> {
    return this.apiService.post<ValidateResponse>('/validation/validate').pipe(
      map((response) => {
        const job: BatchJob = {
          jobId: response.jobId,
          status: response.status,
          statusDisplay: response.statusDisplay,
          createdAt: new Date(response.createdAt),
          totalRecords: response.inputRecordCount,
          successfulRecords: 0,
          failedRecords: 0,
          errorMessage: '',
          resultsFile: ''
        };
        this.currentJobSubject.next(job);
        this.startPollingJob(job.jobId);
        return job;
      })
    );
  }

  /**
   * Get status of a specific job
   */
  getJobStatus(jobId: string): Observable<BatchJob> {
    return this.apiService.get<JobStatusResponse>(`/validation/job/${jobId}`).pipe(
      tap((response) => {
        const job: BatchJob = {
          jobId: response.jobId,
          status: response.status,
          statusDisplay: response.statusDisplay,
          createdAt: new Date(response.createdAt),
          startedAt: response.startedAt ? new Date(response.startedAt) : null,
          completedAt: response.completedAt ? new Date(response.completedAt) : null,
          durationSeconds: response.durationSeconds,
          totalRecords: response.totalRecords,
          successfulRecords: response.successfulRecords,
          failedRecords: response.failedRecords,
          errorMessage: response.errorMessage,
          resultsFile: response.resultsFile
        };
        this.currentJobSubject.next(job);
      })
    );
  }

  /**
   * Get all validation jobs
   */
  getAllJobs(): Observable<any> {
    return this.apiService.get('/validation/jobs');
  }

  /**
   * Get latest validation job
   */
  getLatestJob(): Observable<BatchJob> {
    return this.apiService.get<JobStatusResponse>('/validation/latest').pipe(
      tap((response) => {
        const job: BatchJob = {
          jobId: response.jobId,
          status: response.status,
          statusDisplay: response.statusDisplay,
          createdAt: new Date(response.createdAt),
          startedAt: response.startedAt ? new Date(response.startedAt) : null,
          completedAt: response.completedAt ? new Date(response.completedAt) : null,
          durationSeconds: response.durationSeconds,
          totalRecords: response.totalRecords,
          successfulRecords: response.successfulRecords,
          failedRecords: response.failedRecords,
          errorMessage: response.errorMessage,
          resultsFile: response.resultsFile
        };
        this.currentJobSubject.next(job);
      })
    );
  }

  /**
   * Start polling for job status
   */
  private startPollingJob(jobId: string, intervalMs: number = 2000): void {
    this.isPollingSubject.next(true);
    
    interval(intervalMs)
      .pipe(
        switchMap(() => this.getJobStatus(jobId)),
        takeUntil(this.pollStopSubject)
      )
      .subscribe({
        next: (job) => {
          // Stop polling if job is completed or failed
          if (job.status === 'Completed' || job.status === 'Failed') {
            this.stopPolling();
          }
        },
        error: (err) => {
          console.error('Error polling job status:', err);
          this.stopPolling();
        }
      });
  }

  /**
   * Stop polling for job status
   */
  stopPolling(): void {
    this.pollStopSubject.next();
    this.isPollingSubject.next(false);
  }

  /**
   * Get current job
   */
  getCurrentJob(): BatchJob | null {
    return this.currentJobSubject.value;
  }

  /**
   * Clear current job
   */
  clearCurrentJob(): void {
    this.currentJobSubject.next(null);
    this.stopPolling();
  }
}
