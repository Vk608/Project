import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-banner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="errorMessage" class="alert alert-danger error-banner">
      <div class="error-content">
        <span class="error-icon">⚠️</span>
        <p class="error-message">{{ errorMessage }}</p>
        <button class="close-btn" (click)="onClose()">×</button>
      </div>
    </div>
  `,
  styles: [`
    .error-banner {
      display: flex;
      align-items: center;
    }

    .error-content {
      display: flex;
      align-items: center;
      width: 100%;
      gap: var(--space-sm);
    }

    .error-icon {
      font-size: 20px;
      flex-shrink: 0;
    }

    .error-message {
      margin: 0;
      flex: 1;
    }

    .close-btn {
      background: none;
      border: none;
      color: inherit;
      font-size: 20px;
      cursor: pointer;
      padding: 0;
      width: 24px;
      height: 24px;
      flex-shrink: 0;
      opacity: 0.7;

      &:hover { opacity: 1; }
    }
  `]
})
export class ErrorBannerComponent {
  @Input() errorMessage: string | null = null;
  @Output() closed = new EventEmitter<void>();

  onClose(): void {
    this.closed.emit();
  }
}
