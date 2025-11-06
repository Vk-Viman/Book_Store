import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-admin-promo-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="container mt-4">
    <h2>Create Promo</h2>
    <form (ngSubmit)="submit()">
      <div class="mb-3">
        <label class="form-label">Code</label>
        <input class="form-control" [(ngModel)]="model.code" name="code" required />
        <div *ngIf="submitted && !model.code" class="text-danger small mt-1">Code is required</div>
      </div>
      <div class="mb-3">
        <label class="form-label">Type</label>
        <select class="form-select" [(ngModel)]="model.type" name="type" (change)="onTypeChange()">
          <option value="Percentage">Percentage</option>
          <option value="Fixed">Fixed</option>
          <option value="FreeShipping">FreeShipping</option>
        </select>
      </div>
      <div class="mb-3" *ngIf="model.type === 'Percentage'">
        <label class="form-label">Discount Percent</label>
        <input type="number" step="0.01" class="form-control" [(ngModel)]="model.discountPercent" name="discountPercent" />
        <div *ngIf="submitted && (!model.discountPercent || model.discountPercent <= 0)" class="text-danger small mt-1">Discount percent must be greater than 0</div>
      </div>
      <div class="mb-3" *ngIf="model.type === 'Fixed'">
        <label class="form-label">Fixed Amount</label>
        <input type="number" step="0.01" class="form-control" [(ngModel)]="model.fixedAmount" name="fixedAmount" />
        <div *ngIf="submitted && (!model.fixedAmount || model.fixedAmount <= 0)" class="text-danger small mt-1">Fixed amount must be greater than 0</div>
      </div>
      <div class="mb-3 form-check">
        <input type="checkbox" class="form-check-input" id="activeCheck" [(ngModel)]="model.isActive" name="isActive" />
        <label class="form-check-label" for="activeCheck">Active</label>
      </div>
      <button class="btn btn-primary" [disabled]="!isValid()">Create</button>
    </form>
  </div>
  `
})
export class AdminPromoFormComponent {
  model: any = { code: '', type: 'Percentage', discountPercent: 0, fixedAmount: 0, isActive: true };
  submitted = false;

  constructor(private http: HttpClient, private router: Router) {}

  onTypeChange() {
    // reset values for other types to avoid accidental submission
    if (this.model.type === 'Percentage') { this.model.fixedAmount = 0; }
    if (this.model.type === 'Fixed') { this.model.discountPercent = 0; }
  }

  isValid(): boolean {
    if (!this.model.code || !this.model.code.trim()) return false;
    if (this.model.type === 'Percentage') return !!this.model.discountPercent && this.model.discountPercent > 0;
    if (this.model.type === 'Fixed') return !!this.model.fixedAmount && this.model.fixedAmount > 0;
    // FreeShipping needs only code
    return true;
  }

  submit() {
    this.submitted = true;
    if (!this.isValid()) return;
    this.http.post('/api/admin/promos', this.model).subscribe(() => this.router.navigate(['/admin/promos']), (err) => {
      // show server error (simple alert here)
      alert(err?.error?.message || 'Failed to create promo');
    });
  }
}
