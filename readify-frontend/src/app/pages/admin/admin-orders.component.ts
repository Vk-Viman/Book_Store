import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { adminGuard } from '../../guards/admin.guard';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatSelectModule, MatIconModule, MatCardModule],
  template: `
    <div class="container mt-4">
      <mat-card>
        <mat-card-title>Orders</mat-card-title>
        <mat-card-content>
          <div class="mb-3 d-flex align-items-center">
            <mat-select placeholder="Filter by status" [(value)]="filter" (selectionChange)="load()">
              <mat-option [value]="''">All</mat-option>
              <mat-option value="Processing">Processing</mat-option>
              <mat-option value="Shipped">Shipped</mat-option>
              <mat-option value="Delivered">Delivered</mat-option>
              <mat-option value="Cancelled">Cancelled</mat-option>
              <mat-option value="Paid">Paid</mat-option>
              <mat-option value="Pending">Pending</mat-option>
            </mat-select>
          </div>

          <table mat-table [dataSource]="orders" class="mat-elevation-z8" *ngIf="orders.length>0">
            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef>Id</th>
              <td mat-cell *matCellDef="let o">{{o.id}}</td>
            </ng-container>
            <ng-container matColumnDef="user">
              <th mat-header-cell *matHeaderCellDef>User</th>
              <td mat-cell *matCellDef="let o">{{o.userId}}</td>
            </ng-container>
            <ng-container matColumnDef="date">
              <th mat-header-cell *matHeaderCellDef>Date</th>
              <td mat-cell *matCellDef="let o">{{o.orderDate | date:'short'}}</td>
            </ng-container>
            <ng-container matColumnDef="total">
              <th mat-header-cell *matHeaderCellDef>Total</th>
              <td mat-cell *matCellDef="let o">{{o.totalAmount | currency}}</td>
            </ng-container>
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let o">{{o.orderStatus}} / {{o.paymentStatus}}</td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let o">
                <button mat-button color="primary" (click)="setStatus(o.id, 'Shipped')">Mark Shipped</button>
                <button mat-button color="accent" (click)="setStatus(o.id, 'Delivered')">Mark Delivered</button>
                <button mat-button color="warn" (click)="setStatus(o.id, 'Cancelled')">Cancel</button>
                <button mat-icon-button [routerLink]="['/orders', o.id]" title="View"><mat-icon>open_in_new</mat-icon></button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <div *ngIf="orders.length===0" class="text-center py-4">No orders found.</div>
        </mat-card-content>
      </mat-card>
    </div>
  `
})
export class AdminOrdersComponent {
  orders: any[] = [];
  displayedColumns = ['id', 'user', 'date', 'total', 'status', 'actions'];
  filter = '';

  constructor(private http: HttpClient) { this.load(); }

  load() {
    const q = this.filter ? `?status=${encodeURIComponent(this.filter)}` : '';
    this.http.get<any>(`/api/admin/orders${q}`).subscribe({ next: (res: any) => this.orders = res.items ?? res, error: () => this.orders = [] });
  }

  setStatus(id: number, status: string) {
    this.http.put(`/api/admin/orders/update-status/${id}`, { orderStatus: status }).subscribe({ next: () => this.load(), error: () => alert('Failed to update status') });
  }
}
