import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-validation-summary',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="errors?.length" class="alert alert-danger" role="alert" aria-live="assertive">
      <strong>Please fix the following:</strong>
      <ul>
        <li *ngFor="let e of errors">{{ e }}</li>
      </ul>
    </div>
  `
})
export class ValidationSummaryComponent {
  @Input() errors: string[] = [];
}
