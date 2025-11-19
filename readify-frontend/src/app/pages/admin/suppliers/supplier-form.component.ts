import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-admin-supplier-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="container mt-4">
    <h3>{{ isNew ? 'Create' : 'Edit' }} Supplier</h3>
    <form *ngIf="model" (ngSubmit)="save()">
      <div class="mb-3"><label>Name</label><input class="form-control" [(ngModel)]="model.name" name="name" required /></div>
      <div class="mb-3"><label>Email</label><input class="form-control" [(ngModel)]="model.email" name="email" /></div>
      <div class="mb-3"><label>Phone</label><input class="form-control" [(ngModel)]="model.phone" name="phone" /></div>
      <div class="mb-3"><label>Address</label><textarea class="form-control" [(ngModel)]="model.address" name="address"></textarea></div>
      <div class="mb-3 form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="model.isActive" name="isActive" id="active" /><label class="form-check-label" for="active">Active</label></div>
      <button class="btn btn-primary">Save</button>
    </form>
  </div>
  `
})
export class AdminSupplierFormComponent implements OnInit {
  model: any = { id:0, name:'', email:'', phone:'', address:'', isActive:true };
  isNew = true;
  id: number | null = null;
  constructor(private route: ActivatedRoute, private router: Router, private http: HttpClient) {}

  ngOnInit(): void {
    const p = this.route.snapshot.paramMap.get('id');
    if (!p || p === 'new') { this.isNew = true; return; }
    this.id = Number(p);
    this.isNew = false;
    this.http.get<any>(`/api/admin/suppliers/${this.id}`).subscribe({ next: d => this.model = d, error: () => alert('Failed to load') });
  }

  save() {
    if (this.isNew) {
      this.http.post('/api/admin/suppliers', this.model).subscribe({ next: () => this.router.navigate(['/admin/suppliers']), error: () => alert('Failed to create') });
    } else {
      this.http.put(`/api/admin/suppliers/${this.model.id}`, this.model).subscribe({ next: () => this.router.navigate(['/admin/suppliers']), error: () => alert('Failed to update') });
    }
  }
}
