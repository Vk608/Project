import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ToastService, ToastMessage } from './toast.service';

/**
 * Toast notification renderer.
 * Place once in the root app component:
 *   <app-toast></app-toast>
 */
@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container" *ngIf="toasts.length > 0">
      <div
        *ngFor="let toast of toasts"
        class="toast"
        [ngClass]="'toast-' + toast.type"
        (click)="dismiss(toast.id)"
      >
        <span class="toast-message">{{ toast.message }}</span>
        <button class="toast-close" (click)="dismiss(toast.id); $event.stopPropagation()">×</button>
      </div>
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: calc(var(--navbar-height) + var(--space-sm));
      right: var(--space-md);
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: var(--space-xs);
      max-width: 380px;
    }

    .toast {
      display: flex;
      align-items: center;
      gap: var(--space-sm);
      padding: 8px var(--space-md);
      border-radius: var(--radius-sm);
      font-size: var(--font-md);
      font-weight: 500;
      box-shadow: var(--shadow-lg);
      cursor: pointer;
      animation: toast-enter 0.2s ease;
      border-left: 3px solid;
    }

    .toast-success {
      background: var(--color-success-light);
      color: #14532d;
      border-color: var(--color-success);
    }

    .toast-error {
      background: var(--color-danger-light);
      color: #7f1d1d;
      border-color: var(--color-danger);
    }

    .toast-info {
      background: var(--color-info-light);
      color: #0c3b66;
      border-color: var(--color-info);
    }

    .toast-message {
      flex: 1;
    }

    .toast-close {
      flex-shrink: 0;
      background: none;
      border: none;
      color: inherit;
      font-size: 16px;
      cursor: pointer;
      opacity: 0.5;
      padding: 0;

      &:hover { opacity: 1; }
    }

    @keyframes toast-enter {
      from {
        opacity: 0;
        transform: translateX(20px);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }
  `]
})
export class ToastComponent implements OnInit, OnDestroy {
  toasts: ToastMessage[] = [];
  private destroy$ = new Subject<void>();

  constructor(private toastService: ToastService) {}

  ngOnInit(): void {
    this.toastService.toasts$
      .pipe(takeUntil(this.destroy$))
      .subscribe(toasts => this.toasts = toasts);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  dismiss(id: number): void {
    this.toastService.dismiss(id);
  }
}
