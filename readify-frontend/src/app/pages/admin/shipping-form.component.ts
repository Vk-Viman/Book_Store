import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-admin-shipping-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="container mt-4">
    <h2>Shipping Rates</h2>
    <form *ngIf="model" (ngSubmit)="submit()">
      <div class="mb-3">
        <label class="form-label">Local</label>
        <input type="number" step="0.01" class="form-control" [(ngModel)]="model.local" name="local" />
      </div>
      <div class="mb-3">
        <label class="form-label">National</label>
        <input type="number" step="0.01" class="form-control" [(ngModel)]="model.national" name="national" />
      </div>
      <div class="mb-3">
        <label class="form-label">International</label>
        <input type="number" step="0.01" class="form-control" [(ngModel)]="model.international" name="international" />
      </div>
      <div class="mb-3">
        <label class="form-label">Free shipping threshold</label>
        <input type="number" step="0.01" class="form-control" [(ngModel)]="model.freeShippingThreshold" name="freeShippingThreshold" />
      </div>
      <button class="btn btn-primary">Save</button>
    </form>
  </div>
  `
})
export class AdminShippingFormComponent {
  model: any = null;
  constructor(private http: HttpClient, private router: Router) { this.load(); }
  load() { this.http.get<any>('/api/admin/shipping').subscribe(res => { this.model = { local: res.local, national: res.national, international: res.international, freeShippingThreshold: res.freeShippingThreshold }; }); }
  submit() { this.http.post('/api/admin/shipping', { local: this.model.local, national: this.model.national, international: this.model.international, freeShippingThreshold: this.model.freeShippingThreshold }).subscribe(() => this.router.navigate(['/admin/dashboard'])); }
}
