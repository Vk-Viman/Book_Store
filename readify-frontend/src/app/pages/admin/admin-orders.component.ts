import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule } from '@angular/material/paginator';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatSelectModule, MatIconModule, MatCardModule, MatInputModule, MatPaginatorModule],
  template: `
    <div class="container mt-4">
      <mat-card>
        <mat-card-title>Orders</mat-card-title>
        <mat-card-content>
          <div class="d-flex gap-2 mb-3">
            <mat-input placeholder="Search" [(ngModel)]="q" (keyup.enter)="load()"></mat-input>
            <mat-select [(value)]="filter" (selectionChange)="load()">
              <mat-option value="">All</mat-option>
              <mat-option value="Processing">Processing</mat-option>
              <mat-option value="Shipped">Shipped</mat-option>
              <mat-option value="Delivered">Delivered</mat-option>
              <mat-option value="Cancelled">Cancelled</mat-option>
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
            <ng-container matColumnDef="tx">
              <th mat-header-cell *matHeaderCellDef>Payment Tx</th>
              <td mat-cell *matCellDef="let o">{{o.paymentTransactionId || '-'}}</td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let o">
                <button mat-button color="primary" (click)="confirmSetStatus(o.id, 'Shipped')">Mark Shipped</button>
                <button mat-button color="accent" (click)="confirmSetStatus(o.id, 'Delivered')">Mark Delivered</button>
                <button mat-button color="warn" (click)="confirmSetStatus(o.id, 'Cancelled')">Cancel</button>
                <button mat-icon-button [routerLink]="['/orders', o.id]" title="View"><mat-icon>open_in_new</mat-icon></button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <mat-paginator [length]="total" [pageSize]="pageSize" [pageIndex]="pageIndex" (page)="onPage($event)"></mat-paginator>

          <div *ngIf="orders.length===0" class="text-center py-4">No orders found.</div>
        </mat-card-content>
      </mat-card>
    </div>
  `
})
export class AdminOrdersComponent {
  orders: any[] = [];
  displayedColumns = ['id', 'user', 'date', 'total', 'status', 'tx', 'actions'];
  filter = '';
  q = '';
  pageSize = 10;
  pageIndex = 0;
  total = 0;

  constructor(private http: HttpClient) { this.load(); }

  load() {
    const page = this.pageIndex + 1;
    const q = this.q ? `&q=${encodeURIComponent(this.q)}` : '';
    const s = this.filter ? `&status=${encodeURIComponent(this.filter)}` : '';
    this.http.get<any>(`/api/admin/orders?page=${page}&pageSize=${this.pageSize}${q}${s}`).subscribe({ next: (r: any) => { this.orders = r.items ?? []; this.total = r.total ?? 0; this.pageIndex = (r.page ?? page) - 1; this.pageSize = r.pageSize ?? this.pageSize; }, error: () => { this.orders = []; this.total = 0; } });
  }

  onPage(ev: any) { this.pageIndex = ev.pageIndex; this.pageSize = ev.pageSize; this.load(); }

  confirmSetStatus(id: number, status: string) {
    if (!confirm(`Set order ${id} status to ${status}?`)) return;
    this.setStatus(id, status);
  }

  setStatus(id: number, status: string) {
    this.http.put(`/api/admin/orders/update-status/${id}`, { orderStatus: status }).subscribe({ next: () => this.load(), error: () => alert('Failed to update status') });
  }
}
