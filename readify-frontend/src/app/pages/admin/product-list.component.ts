import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-admin-product-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatChipsModule
  ],
  template: `
  <div class="container mt-4">
    <mat-card>
      <mat-card-header class="mb-3">
        <mat-card-title class="d-flex justify-content-between align-items-center w-100">
          <div class="title-section">
            <mat-icon class="me-2">inventory_2</mat-icon>
            <span>Products Management</span>
          </div>
          <button mat-raised-button color="primary" (click)="create()">
            <mat-icon>add</mat-icon>
            Add Product
          </button>
        </mat-card-title>
      </mat-card-header>

      <mat-card-content>
        <!-- Search Bar -->
        <mat-form-field appearance="outline" class="w-100 mb-3">
          <mat-label>Search products</mat-label>
          <input matInput [(ngModel)]="searchTerm" (keyup)="filterProducts()" placeholder="Search by title or author...">
          <mat-icon matPrefix>search</mat-icon>
          <button mat-icon-button matSuffix *ngIf="searchTerm" (click)="searchTerm=''; filterProducts()">
            <mat-icon>clear</mat-icon>
          </button>
        </mat-form-field>

        <!-- Loading State -->
        <div *ngIf="loading" class="text-center py-5">
          <mat-spinner></mat-spinner>
        </div>

        <!-- Products Table -->
        <div *ngIf="!loading && filteredProducts.length > 0" class="table-responsive">
          <table mat-table [dataSource]="filteredProducts" class="w-100">
            <!-- Title Column -->
            <ng-container matColumnDef="title">
              <th mat-header-cell *matHeaderCellDef>Title</th>
              <td mat-cell *matCellDef="let p">
                <div class="product-cell">
                  <img [src]="p.imageUrl || 'assets/book-placeholder.svg'" class="product-thumbnail" [alt]="p.title">
                  <span class="ms-2">{{ p.title }}</span>
                </div>
              </td>
            </ng-container>

            <!-- Authors Column -->
            <ng-container matColumnDef="authors">
              <th mat-header-cell *matHeaderCellDef>Authors</th>
              <td mat-cell *matCellDef="let p">{{ p.authors }}</td>
            </ng-container>

            <!-- Price Column -->
            <ng-container matColumnDef="price">
              <th mat-header-cell *matHeaderCellDef>Price</th>
              <td mat-cell *matCellDef="let p">
                <strong class="price-text">{{ p.price | currency }}</strong>
              </td>
            </ng-container>

            <!-- Stock Column -->
            <ng-container matColumnDef="stock">
              <th mat-header-cell *matHeaderCellDef>Stock</th>
              <td mat-cell *matCellDef="let p">
                <mat-chip-set>
                  <mat-chip [class.low-stock]="p.stockQty < 5" [class.out-of-stock]="p.stockQty === 0">
                    {{ p.stockQty }}
                  </mat-chip>
                </mat-chip-set>
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let p">
                <button mat-icon-button color="primary" (click)="edit(p.id)" matTooltip="Edit">
                  <mat-icon>edit</mat-icon>
                </button>
                <button mat-icon-button color="warn" (click)="delete(p.id)" matTooltip="Delete">
                  <mat-icon>delete</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        </div>

        <!-- Empty State -->
        <div *ngIf="!loading && filteredProducts.length === 0" class="text-center py-5">
          <mat-icon class="empty-icon">inventory</mat-icon>
          <h3 class="mt-3">No products found</h3>
          <p class="text-muted" *ngIf="searchTerm">Try adjusting your search term</p>
          <p class="text-muted" *ngIf="!searchTerm">Get started by adding your first product</p>
          <button mat-raised-button color="primary" (click)="create()" class="mt-3">
            <mat-icon>add</mat-icon>
            Add Product
          </button>
        </div>
      </mat-card-content>
    </mat-card>
  </div>
  `,
  styles: [`
    mat-card-title {
      display: flex;
      align-items: center;
    }
    .title-section {
      display: flex;
      align-items: center;
    }
    .product-cell {
      display: flex;
      align-items: center;
    }
    .product-thumbnail {
      width: 40px;
      height: 40px;
      object-fit: cover;
      border-radius: 4px;
    }
    .price-text {
      color: var(--primary-color);
      font-size: 1.1rem;
    }
    .low-stock {
      background-color: #fff3e0;
      color: #e65100;
    }
    .out-of-stock {
      background-color: #ffebee;
      color: #c62828;
    }
    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #ccc;
    }
    table {
      width: 100%;
    }
    th {
      font-weight: 600;
    }
  `]
})
export class AdminProductListComponent {
  products: any[] = [];
  filteredProducts: any[] = [];
  searchTerm = '';
  loading = false;
  displayedColumns: string[] = ['title', 'authors', 'price', 'stock', 'actions'];

  constructor(
    private http: HttpClient, 
    private router: Router,
    private notify: NotificationService
  ) { 
    this.load(); 
  }

  load() { 
    this.loading = true;
    this.http.get('/api/products').subscribe({
      next: (res: any) => { 
        this.products = res.items ?? res;
        this.filteredProducts = this.products;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.notify.error('Failed to load products');
      }
    });
  }

  filterProducts() {
    if (!this.searchTerm.trim()) {
      this.filteredProducts = this.products;
      return;
    }
    const term = this.searchTerm.toLowerCase();
    this.filteredProducts = this.products.filter(p => 
      p.title?.toLowerCase().includes(term) || 
      p.authors?.toLowerCase().includes(term)
    );
  }

  create() { 
    this.router.navigate(['/admin/products/new']); 
  }

  edit(id: number) { 
    this.router.navigate(['/admin/products', id]); 
  }

  delete(id: number) {
    if (!confirm('Are you sure you want to delete this product? This action cannot be undone.')) return;
    this.http.delete(`/api/admin/products/${id}`).subscribe({
      next: () => {
        this.notify.success('Product deleted successfully');
        this.load();
      },
      error: () => {
        this.notify.error('Failed to delete product');
      }
    });
  }
}
