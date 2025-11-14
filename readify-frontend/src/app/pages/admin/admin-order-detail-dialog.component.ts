import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { LocalDatePipe } from '../../pipes/local-date.pipe';
import { HttpClient } from '@angular/common/http';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-admin-order-detail-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatCardModule, MatListModule, MatButtonModule, LocalDatePipe, MatProgressSpinnerModule, RouterLink],
  template: `
    <mat-card class="order-detail-dialog">
      <mat-card-title>Order #{{data?.id}}</mat-card-title>
      <mat-card-subtitle class="mb-2">Status: <strong>{{ data?.orderStatus || data?.orderStatusString || data?.status }}</strong></mat-card-subtitle>
      <mat-card-content>
        <div class="row">
          <div class="col-md-6">
            <h6>Shipping</h6>
            <p *ngIf="data?.shippingName"><strong>Name:</strong> {{ data?.shippingName }}</p>
            <p *ngIf="data?.shippingAddress"><strong>Address:</strong> {{ data?.shippingAddress }}</p>
            <p *ngIf="data?.shippingPhone"><strong>Phone:</strong> {{ data?.shippingPhone }}</p>
          </div>

          <div class="col-md-6 text-md-end mt-0">
            <h6>User</h6>
            <div *ngIf="loadingUser" class="py-2 text-center"><mat-spinner diameter="24"></mat-spinner></div>
            <div *ngIf="!loadingUser && user">
              <div><strong>{{user.fullName || user.email}}</strong></div>
              <div class="text-muted small">{{user.email}}</div>
              <div class="text-muted small">Role: {{user.role}}</div>
            </div>
            <div *ngIf="!loadingUser && user === null" class="text-muted small">User info not available.</div>
            <h6 class="mt-2">Payment</h6>
            <p *ngIf="data?.paymentTransactionId"><strong>Transaction:</strong> {{ data?.paymentTransactionId }}</p>
            <p><strong>Total:</strong> {{ data?.totalAmount | currency }}</p>
          </div>
        </div>

        <h5 class="mt-3">Items</h5>
        <mat-list *ngIf="data?.items?.length>0">
          <mat-list-item *ngFor="let it of data.items">
            <div style="width:100%" class="d-flex justify-content-between align-items-center">
              <div>
                <div><strong>{{ it.product?.title || it.productName || ('Product #' + it.productId) }}</strong></div>
                <div class="text-muted small">Qty: {{ it.quantity }} — Unit: {{ it.unitPrice | currency }}</div>
              </div>
              <div class="text-end"><strong>{{ (it.quantity * it.unitPrice) | currency }}</strong></div>
            </div>
          </mat-list-item>
        </mat-list>
        <div *ngIf="!data?.items || data.items.length===0" class="text-muted py-2">No items available</div>

        <div class="mt-3 text-end"><strong>Total: {{ data?.totalAmount | currency }}</strong></div>
      </mat-card-content>
      <mat-card-actions class="d-flex justify-content-end">
        <a *ngIf="user" mat-stroked-button [routerLink]="['/admin/users', user.id]">View user</a>
        <button mat-stroked-button (click)="close()">Close</button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: [`
    .order-detail-dialog { width: 700px; max-width: 95vw; }
    @media (max-width:767px) { .order-detail-dialog { width: 100%; } }
  `]
})
export class AdminOrderDetailDialogComponent implements OnInit {
  user: any | null = null;
  loadingUser = false;

  constructor(@Inject(MAT_DIALOG_DATA) public data: any, private dialogRef: MatDialogRef<AdminOrderDetailDialogComponent>, private http: HttpClient) {}

  ngOnInit(): void {
    // load user info if userId is present
    const uid = this.data?.userId ?? this.data?.userId;
    if (uid) {
      this.loadingUser = true;
      this.http.get<any>(`/api/admin/users/${uid}`).subscribe({ next: (res) => { this.user = res; this.loadingUser = false; }, error: () => { this.user = null; this.loadingUser = false; } });
    }
  }

  close() { this.dialogRef.close(); }
}
