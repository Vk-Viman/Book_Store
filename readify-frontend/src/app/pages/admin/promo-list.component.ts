import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-admin-promo-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
  <div class="container mt-4">
    <h2>Promo Codes</h2>
    <div class="row mb-3">
      <div class="col-md-6">
        <input class="form-control" [(ngModel)]="q" placeholder="Search by code" (keyup.enter)="load()" />
      </div>
      <div class="col-md-6 text-end">
        <a routerLink="/admin/promos/new" class="btn btn-primary">Create Promo</a>
      </div>
    </div>
    <table class="table table-striped">
      <thead>
        <tr><th>Code</th><th>Type</th><th>Percent</th><th>Fixed</th><th>Active</th><th>Actions</th></tr>
      </thead>
      <tbody>
        <tr *ngFor="let p of promos">
          <td>{{ p.code }}</td>
          <td>{{ p.type }}</td>
          <td>{{ p.discountPercent }}</td>
          <td>{{ p.fixedAmount }}</td>
          <td>{{ p.isActive }}</td>
          <td>
            <a class="btn btn-sm btn-outline-primary me-2" [routerLink]="['/admin/promos', p.id]">Edit</a>
            <button class="btn btn-danger btn-sm" (click)="delete(p.id)">Delete</button>
          </td>
        </tr>
      </tbody>
    </table>

    <nav *ngIf="totalPages > 1">
      <ul class="pagination">
        <li class="page-item" [class.disabled]="page === 1"><button class="page-link" (click)="goto(page-1)">Previous</button></li>
        <li class="page-item" *ngFor="let p of pages" [class.active]="p===page"><button class="page-link" (click)="goto(p)">{{p}}</button></li>
        <li class="page-item" [class.disabled]="page === totalPages"><button class="page-link" (click)="goto(page+1)">Next</button></li>
      </ul>
    </nav>
  </div>
  `
})
export class AdminPromoListComponent {
  promos: any[] = [];
  q = '';
  page = 1;
  pageSize = 10;
  totalPages = 0;
  pages: number[] = [];

  constructor(private http: HttpClient) { this.load(); }

  load() {
    let params = new HttpParams().set('page', String(this.page)).set('pageSize', String(this.pageSize));
    if (this.q) params = params.set('q', this.q);
    this.http.get<any>('/api/admin/promos', { params }).subscribe((res: any) => {
      this.promos = res.items || res;
      this.totalPages = res.totalPages || 1;
      this.pages = Array.from({ length: this.totalPages }, (_, i) => i + 1);
    });
  }

  goto(p: number) { if (p < 1 || p > this.totalPages) return; this.page = p; this.load(); }

  delete(id: number) { if (!confirm('Delete promo?')) return; this.http.delete(`/api/admin/promos/${id}`).subscribe(() => this.load()); }
}
