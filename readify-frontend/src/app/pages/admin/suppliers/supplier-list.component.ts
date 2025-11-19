import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-admin-supplier-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
  <div class="container mt-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h3>Suppliers</h3>
      <button class="btn btn-primary" (click)="createNew()">Add Supplier</button>
    </div>

    <table class="table table-striped">
      <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Active</th><th>Actions</th></tr></thead>
      <tbody>
        <tr *ngFor="let s of suppliers">
          <td>{{s.name}}</td>
          <td>{{s.email}}</td>
          <td>{{s.phone}}</td>
          <td>{{s.isActive ? 'Yes' : 'No'}}</td>
          <td>
            <a [routerLink]="['/admin/suppliers', s.id]" class="btn btn-sm btn-outline-primary me-2">Edit</a>
            <button class="btn btn-sm btn-danger" (click)="deactivate(s)">Deactivate</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
  `
})
export class AdminSupplierListComponent {
  suppliers: any[] = [];
  constructor(private http: HttpClient, private router: Router) { this.load(); }
  load() { this.http.get<any[]>('/api/admin/suppliers').subscribe({ next: d => this.suppliers = d || [], error: () => this.suppliers = [] }); }
  createNew() { this.router.navigate(['/admin/suppliers/new']); }
  deactivate(s: any) { if(!confirm('Deactivate supplier?')) return; this.http.delete(`/api/admin/suppliers/${s.id}`).subscribe({ next: () => this.load(), error: () => alert('Failed to deactivate') }); }
}
