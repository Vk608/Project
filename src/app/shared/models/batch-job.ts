/**
 * Represents a batch validation job with status tracking
 */
export interface BatchJob {
  jobId: string;
  status: JobStatus | string;
  statusDisplay: string;
  createdAt: string | Date;
  startedAt?: string | Date | null;
  completedAt?: string | Date | null;
  durationSeconds?: number | null;
  totalRecords: number;
  successfulRecords: number;
  failedRecords: number;
  errorMessage: string;
  resultsFile: string;
}

/**
 * Job status enumeration
 */
export enum JobStatus {
  PENDING = 'Pending',
  RUNNING = 'Running',
  COMPLETED = 'Completed',
  FAILED = 'Failed'
}

/**
 * API Response interfaces
 */
export interface ValidateResponse {
  jobId: string;
  status: string;
  statusDisplay: string;
  createdAt: string;
  message: string;
  inputRecordCount: number;
}

export interface JobStatusResponse {
  jobId: string;
  status: string;
  statusDisplay: string;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
  durationSeconds?: number | null;
  totalRecords: number;
  successfulRecords: number;
  failedRecords: number;
  errorMessage: string;
  resultsFile: string;
}

export interface JobsListResponse {
  count: number;
  jobs: Array<{
    jobId: string;
    status: string;
    statusDisplay: string;
    createdAt: string;
    completedAt?: string | null;
    durationSeconds?: number | null;
    successfulRecords: number;
    failedRecords: number;
  }>;
}
