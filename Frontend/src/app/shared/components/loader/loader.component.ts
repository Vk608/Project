import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Reusable loader/spinner component.
 *
 * Usage:
 *   <app-loader message="Loading records..."></app-loader>
 *   <app-loader></app-loader>
 */
@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="loading-state">
      <div class="spinner"></div>
      <p *ngIf="message">{{ message }}</p>
    </div>
  `
})
export class LoaderComponent {
  @Input() message = '';
}
