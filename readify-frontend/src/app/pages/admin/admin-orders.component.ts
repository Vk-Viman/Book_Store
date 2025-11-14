import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule } from '@angular/material/paginator';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../components/confirm-dialog.component';
import { NotificationService } from '../../services/notification.service';
import { AdminOrderDetailDialogComponent } from './admin-order-detail-dialog.component';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatSelectModule, MatIconModule, MatCardModule, MatInputModule, MatFormFieldModule, MatPaginatorModule, FormsModule, ConfirmDialogComponent, MatChipsModule, AdminOrderDetailDialogComponent],
  template: `
    <div class="container mt-4">
      <mat-card>
        <mat-card-title>Orders</mat-card-title>
        <mat-card-content>
          <div class="d-flex gap-2 mb-3">
            <mat-form-field appearance="outline" class="flex-grow-1">
              <mat-label>Search</mat-label>
              <input matInput placeholder="Search" [(ngModel)]="q" (keyup.enter)="load()" />
            </mat-form-field>
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
              <td mat-cell *matCellDef="let o">
                <mat-form-field appearance="outline">
                  <mat-select [(value)]="o.orderStatus" (selectionChange)="onStatusChange(o, $event.value)">
                    <mat-option value="Pending">Pending</mat-option>
                    <mat-option value="Processing">Processing</mat-option>
                    <mat-option value="Shipped">Shipped</mat-option>
                    <mat-option value="Delivered">Delivered</mat-option>
                    <mat-option value="Cancelled">Cancelled</mat-option>
                  </mat-select>
                </mat-form-field>
                <div class="mt-1">
                  <mat-chip role="button" tabindex="0" (click)="filterByStatus(o.orderStatus || o.orderStatusString || o.status)" [ngClass]="getStatusClass(o.orderStatus || o.orderStatusString || o.status)">{{ mapStatusLabel(o.orderStatus || o.orderStatusString || o.status) }}</mat-chip>
                  <div class="text-muted small">Payment: {{o.paymentStatus}}</div>
                </div>
              </td>
            </ng-container>
            <ng-container matColumnDef="tx">
              <th mat-header-cell *matHeaderCellDef>Payment Tx</th>
              <td mat-cell *matCellDef="let o">{{o.paymentTransactionId || '-'}}</td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let o">
                <button mat-icon-button (click)="openDetail(o)" title="View"><mat-icon>open_in_new</mat-icon></button>
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

  constructor(private http: HttpClient, private dialog: MatDialog, private notify: NotificationService) { this.load(); }

  load() {
    const page = this.pageIndex + 1;
    const q = this.q ? `&q=${encodeURIComponent(this.q)}` : '';
    const s = this.filter ? `&status=${encodeURIComponent(this.filter)}` : '';
    this.http.get<any>(`/api/admin/orders?page=${page}&pageSize=${this.pageSize}${q}${s}`).subscribe({ next: (r: any) => { this.orders = r.items ?? []; this.total = r.total ?? 0; this.pageIndex = (r.page ?? page) - 1; this.pageSize = r.pageSize ?? this.pageSize; }, error: () => { this.orders = []; this.total = 0; } });
  }

  onPage(ev: any) { this.pageIndex = ev.pageIndex; this.pageSize = ev.pageSize; this.load(); }

  onStatusChange(order: any, newStatus: string) {
    const prev = order.orderStatus;
    const ref = this.dialog.open(ConfirmDialogComponent, { data: { title: 'Change Order Status', message: `Change status to ${newStatus}?` } });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) {
        // revert selection in UI
        order.orderStatus = prev;
        return;
      }

      order.orderStatus = newStatus;
      this.http.put(`/api/admin/orders/${order.id}/status`, { orderStatus: newStatus }).subscribe({ next: () => { this.notify.success('Order status updated'); this.load(); }, error: () => { order.orderStatus = prev; this.notify.error('Failed to update status'); } });
    });
  }

  openDetail(o: any) { this.dialog.open(AdminOrderDetailDialogComponent, { data: o }); }

  mapStatusLabel(s: any) { if (!s) return ''; const st = s.toString().split('.').pop(); return st.charAt(0).toUpperCase() + st.slice(1).toLowerCase(); }
  getStatusClass(s: any) {
    const st = (s || '').toString().toLowerCase();
    if (st.includes('pending')) return 'status-pending';
    if (st.includes('shipped')) return 'status-shipped';
    if (st.includes('delivered')) return 'status-delivered';
    return 'status-default';
  }

  filterByStatus(s: any) { this.filter = this.mapStatusLabel(s); this.pageIndex = 0; this.load(); }
}
