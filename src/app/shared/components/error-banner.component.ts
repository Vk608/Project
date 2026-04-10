import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-banner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="errorMessage" class="error-banner">
      <div class="error-content">
        <span class="error-icon">⚠️</span>
        <p class="error-message">{{ errorMessage }}</p>
        <button class="close-btn" (click)="onClose()">×</button>
      </div>
    </div>
  `,
  styles: [`
    .error-banner {
      background-color: #ffebee;
      border-left: 4px solid #c62828;
      padding: 12px 16px;
      margin-bottom: 16px;
      border-radius: 4px;
      display: flex;
      align-items: center;
    }

    .error-content {
      display: flex;
      align-items: center;
      width: 100%;
      gap: 12px;
    }

    .error-icon {
      font-size: 20px;
      flex-shrink: 0;
    }

    .error-message {
      margin: 0;
      color: #c62828;
      flex: 1;
    }

    .close-btn {
      background: none;
      border: none;
      color: #c62828;
      font-size: 20px;
      cursor: pointer;
      padding: 0;
      width: 24px;
      height: 24px;
      flex-shrink: 0;
    }

    .close-btn:hover {
      opacity: 0.7;
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
