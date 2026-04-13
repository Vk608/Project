import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Reusable card component.
 * Provides consistent card styling from the design system.
 *
 * Usage:
 *   <app-card cardTitle="My Section">
 *     <p>Card content goes here</p>
 *   </app-card>
 *
 *   <app-card>
 *     <p>Card without title</p>
 *   </app-card>
 */
@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div *ngIf="cardTitle" class="card-header">
        <h3 class="card-title">{{ cardTitle }}</h3>
        <ng-content select="[card-actions]"></ng-content>
      </div>
      <ng-content></ng-content>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class CardComponent {
  @Input() cardTitle: string = '';
}
