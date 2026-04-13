import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

/**
 * Reusable input field with label and validation.
 *
 * Usage:
 *   <app-input-field
 *     label="Search"
 *     placeholder="Enter keyword..."
 *     [(ngModel)]="searchTerm"
 *     [errorMessage]="'This field is required'"
 *   ></app-input-field>
 */
@Component({
  selector: 'app-input-field',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputFieldComponent),
      multi: true
    }
  ],
  template: `
    <div class="form-group">
      <label *ngIf="label" class="form-label" [attr.for]="inputId">{{ label }}</label>
      <input
        [id]="inputId"
        [type]="type"
        [placeholder]="placeholder"
        [disabled]="isDisabled"
        class="input-field"
        [class.input-error]="errorMessage"
        [ngModel]="value"
        (ngModelChange)="onValueChange($event)"
        (blur)="onTouched()"
      />
      <p *ngIf="errorMessage" class="form-error">{{ errorMessage }}</p>
    </div>
  `,
  styles: [`
    .input-error {
      border-color: var(--color-danger) !important;
    }

    .input-error:focus {
      box-shadow: 0 0 0 3px var(--color-danger-light) !important;
    }
  `]
})
export class InputFieldComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() type: 'text' | 'email' | 'password' | 'number' = 'text';
  @Input() errorMessage: string | null = null;
  @Input() inputId = `input-${Math.random().toString(36).substring(2, 9)}`;

  value = '';
  isDisabled = false;

  private onChange: (value: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(value: string): void {
    this.value = value || '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  onValueChange(newValue: string): void {
    this.value = newValue;
    this.onChange(newValue);
  }
}
