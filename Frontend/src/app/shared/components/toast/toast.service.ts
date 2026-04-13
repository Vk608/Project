import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ToastMessage {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
}

/**
 * Service for showing toast notifications.
 *
 * Usage:
 *   constructor(private toast: ToastService) {}
 *   this.toast.show('Record saved!', 'success');
 *   this.toast.show('Something went wrong', 'error');
 */
@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
  public toasts$: Observable<ToastMessage[]> = this.toastsSubject.asObservable();

  private nextId = 0;

  show(message: string, type: 'success' | 'error' | 'info' = 'info'): void {
    const toast: ToastMessage = {
      id: this.nextId++,
      message,
      type
    };

    const current = this.toastsSubject.value;
    this.toastsSubject.next([...current, toast]);

    // Auto-dismiss after 4 seconds
    setTimeout(() => this.dismiss(toast.id), 4000);
  }

  dismiss(id: number): void {
    const filtered = this.toastsSubject.value.filter(t => t.id !== id);
    this.toastsSubject.next(filtered);
  }
}
