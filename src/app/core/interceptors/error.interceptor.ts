import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

/**
 * HTTP Error Interceptor for consistent error handling
 */
@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        console.error('HTTP Error:', error);

        const errorMessage = this.getErrorMessage(error);

        return throwError(() => ({
          status: error.status,
          message: errorMessage,
          error: error.error
        }));
      })
    );
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Unable to connect to the server. Please check if the backend is running.';
    }

    if (error.status === 400) {
      return error.error?.error || 'Bad request. Please check your input.';
    }

    if (error.status === 404) {
      return 'Resource not found.';
    }

    if (error.status === 500) {
      return error.error?.error || 'Server error. Please try again later.';
    }

    return error.error?.message || error.statusText || 'An error occurred';
  }
}
