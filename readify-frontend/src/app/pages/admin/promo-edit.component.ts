import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-admin-promo-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="container mt-4">
    <h2>Edit Promo</h2>
    <form *ngIf="model" (ngSubmit)="submit()">
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
      <div class="mb-3">
        <label class="form-label">Expiry Date (UTC)</label>
        <input type="date" class="form-control" [(ngModel)]="model.expiryDate" name="expiryDate" />
      </div>
      <div class="mb-3">
        <label class="form-label">Minimum Purchase</label>
        <input type="number" step="0.01" class="form-control" [(ngModel)]="model.minPurchase" name="minPurchase" />
      </div>
      <div class="mb-3">
        <label class="form-label">Global Usage Limit</label>
        <input type="number" class="form-control" [(ngModel)]="model.globalUsageLimit" name="globalUsageLimit" />
      </div>
      <div class="mb-3">
        <label class="form-label">Per-User Limit</label>
        <input type="number" class="form-control" [(ngModel)]="model.perUserLimit" name="perUserLimit" />
      </div>
      <div class="mb-3 form-check">
        <input type="checkbox" class="form-check-input" id="activeCheck" [(ngModel)]="model.isActive" name="isActive" />
        <label class="form-check-label" for="activeCheck">Active</label>
      </div>
      <button class="btn btn-primary" [disabled]="!isValid()">Save</button>
    </form>
  </div>
  `
})
export class AdminPromoEditComponent implements OnInit {
  model: any = null;
  id: number | null = null;
  submitted = false;

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.id = Number(this.route.snapshot.paramMap.get('id'));
    if (this.id) this.load();
  }

  load() { this.http.get(`/api/admin/promos/${this.id}`).subscribe((res:any) => this.model = res); }

  onTypeChange() {
    if (!this.model) return;
    if (this.model.type === 'Percentage') { this.model.fixedAmount = 0; }
    if (this.model.type === 'Fixed') { this.model.discountPercent = 0; }
  }

  isValid(): boolean {
    if (!this.model) return false;
    if (!this.model.code || !this.model.code.trim()) return false;
    if (this.model.type === 'Percentage') return !!this.model.discountPercent && this.model.discountPercent > 0;
    if (this.model.type === 'Fixed') return !!this.model.fixedAmount && this.model.fixedAmount > 0;
    return true;
  }

  submit() {
    this.submitted = true;
    if (!this.isValid()) return;
    this.http.put(`/api/admin/promos/${this.id}`, this.model).subscribe(() => this.router.navigate(['/admin/promos']), (err) => {
      alert(err?.error?.message || 'Failed to update promo');
    });
  }
}
