import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Reusable button component.
 * Uses global .btn classes from the design system.
 *
 * Usage:
 *   <app-button variant="primary" (clicked)="onSubmit()">Submit</app-button>
 *   <app-button variant="secondary" size="sm" [disabled]="true">Cancel</app-button>
 *   <app-button variant="primary" [loading]="true">Saving...</app-button>
 */
@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [type]="type"
      class="btn"
      [ngClass]="cssClasses"
      [disabled]="disabled || loading"
      (click)="onClick($event)"
    >
      <span *ngIf="loading" class="btn-spinner"></span>
      <ng-content></ng-content>
    </button>
  `,
  styles: [`
    .btn-spinner {
      display: inline-block;
      width: 12px;
      height: 12px;
      border: 1.5px solid rgba(255, 255, 255, 0.35);
      border-top-color: currentColor;
      border-radius: 50%;
      animation: btn-spin 0.7s linear infinite;
    }

    @keyframes btn-spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class ButtonComponent {
  @Input() variant: 'primary' | 'secondary' | 'danger' = 'primary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() type: 'button' | 'submit' = 'button';
  @Input() disabled = false;
  @Input() loading = false;
  @Output() clicked = new EventEmitter<MouseEvent>();

  get cssClasses(): string[] {
    const classes = [`btn-${this.variant}`];
    if (this.size !== 'md') {
      classes.push(`btn-${this.size}`);
    }
    return classes;
  }

  onClick(event: MouseEvent): void {
    if (!this.disabled && !this.loading) {
      this.clicked.emit(event);
    }
  }
}
