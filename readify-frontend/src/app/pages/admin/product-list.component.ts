import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../services/product.service';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { NotificationService } from '../../services/notification.service';
import { LocalDatePipe } from '../../pipes/local-date.pipe';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../components/confirm-dialog.component';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule, MatIconModule, RouterModule, MatButtonModule, LocalDatePipe, ConfirmDialogComponent],
  template: `
  <div class="container mt-4">
    <mat-card>
      <div class="d-flex align-items-center justify-content-between mb-2">
        <mat-card-title>Products</mat-card-title>
        <div>
          <button mat-raised-button color="primary" (click)="createProduct()">+ Create Product</button>
        </div>
      </div>
      <mat-card-content>
        <table mat-table [dataSource]="products" class="w-100">
          <ng-container matColumnDef="id">
            <th mat-header-cell *matHeaderCellDef>ID</th>
            <td mat-cell *matCellDef="let p">{{ p.id }}</td>
          </ng-container>

          <ng-container matColumnDef="title">
            <th mat-header-cell *matHeaderCellDef>Title</th>
            <td mat-cell *matCellDef="let p"><a [routerLink]="['/admin/products', p.id]">{{ p.title }}</a></td>
          </ng-container>

          <ng-container matColumnDef="created">
            <th mat-header-cell *matHeaderCellDef>Created</th>
            <td mat-cell *matCellDef="let p">{{ p.createdAt | localDate:'medium' }}</td>
          </ng-container>

          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>Actions</th>
            <td mat-cell *matCellDef="let p">
              <button mat-icon-button color="primary" aria-label="Edit" (click)="editProduct(p.id)">
                <mat-icon>edit</mat-icon>
              </button>
              <button mat-icon-button color="warn" aria-label="Delete" (click)="deleteProduct(p.id)" [disabled]="deleting.has(p.id)">
                <mat-icon *ngIf="!deleting.has(p.id)">delete</mat-icon>
                <mat-icon *ngIf="deleting.has(p.id)">hourglass_top</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="['id','title','created','actions']"></tr>
          <tr mat-row *matRowDef="let row; columns: ['id','title','created','actions'];"></tr>
        </table>
      </mat-card-content>
    </mat-card>
  </div>
  `
})
export class AdminProductListComponent {
  products: any[] = [];
  loading = false;
  deleting = new Set<number>();

  constructor(private product: ProductService, private router: Router, private notify: NotificationService, private dialog: MatDialog) { this.load(); }

  load() { this.loading = true; this.product.getProducts().subscribe({ next: (res: any) => { this.products = res.items || []; this.loading = false; }, error: (_err: any) => { this.loading = false; } }); }

  createProduct() {
    this.router.navigate(['/admin/products', 'new']);
  }

  editProduct(id: number) {
    this.router.navigate(['/admin/products', id]);
  }

  deleteProduct(id: number) {
    const ref = this.dialog.open(ConfirmDialogComponent, { data: { title: 'Delete Product', message: 'Delete this product? This action cannot be undone.' } });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;

      const idx = this.products.findIndex(p => p.id === id);
      const backup = idx >= 0 ? this.products[idx] : null;

      if (idx >= 0) this.products.splice(idx, 1);
      this.deleting.add(id);

      this.product.deleteProduct(id).subscribe({
        next: (_res: any) => {
          this.deleting.delete(id);
          this.notify.success('Product deleted');
          this.load();
        },
        error: (err: any) => {
          this.deleting.delete(id);
          if (backup) this.products.splice(idx, 0, backup);
          this.notify.error(err?.error?.message || 'Failed to delete product');
        }
      });
    });
  }
}
