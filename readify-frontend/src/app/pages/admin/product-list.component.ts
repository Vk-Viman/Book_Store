import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-product-list',
  standalone: true,
  imports: [CommonModule],
  template: `
  <div class="container mt-4">
    <div class="d-flex justify-content-between mb-3">
      <h3>Products</h3>
      <button class="btn btn-success" (click)="create()">Add product</button>
    </div>
    <table class="table table-striped">
      <thead><tr><th>Title</th><th>Authors</th><th>Price</th><th>Stock</th><th></th></tr></thead>
      <tbody>
        <tr *ngFor="let p of products">
          <td>{{p.title}}</td>
          <td>{{p.authors}}</td>
          <td>{{p.price | currency}}</td>
          <td>{{p.stockQty}}</td>
          <td>
            <button class="btn btn-sm btn-primary me-2" (click)="edit(p.id)">Edit</button>
            <button class="btn btn-sm btn-danger" (click)="delete(p.id)">Delete</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
  `
})
export class AdminProductListComponent {
  products: any[] = [];

  constructor(private http: HttpClient, private router: Router) { this.load(); }

  load() { this.http.get('/api/products').subscribe((res: any) => { this.products = res.items ?? res; }); }

  create() { this.router.navigate(['/admin/products/new']); }
  edit(id: number) { this.router.navigate(['/admin/products', id]); }

  delete(id: number) {
    if (!confirm('Delete this product?')) return;
    this.http.delete(`/api/admin/products/${id}`).subscribe(() => this.load());
  }
}
