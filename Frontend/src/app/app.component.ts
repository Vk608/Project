import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RecordsListComponent } from './features/records/records-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RecordsListComponent],
  template: `<app-records-list></app-records-list>`,
  styles: []
})
export class AppComponent {
  title = 'PubMed Validation Dashboard';
}
