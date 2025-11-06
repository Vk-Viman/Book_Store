import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { NotificationService } from '../../services/notification.service';
import { LocalDatePipe } from '../../pipes/local-date.pipe';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatListModule, LocalDatePipe],
  template: `
  <div class="container mt-4">
    <button class="btn btn-link mb-2" (click)="back()">← Back to orders</button>
    <mat-card *ngIf="order" class="order-detail-card">
      <mat-card-title>Order #{{ order.id }} — {{ order.createdAt | localDate:'medium' }}</mat-card-title>
      <mat-card-subtitle class="mb-2">Status: <strong>{{ order.status }}</strong></mat-card-subtitle>
      <mat-card-content class="mt-2">
        <div class="row">
          <div class="col-md-6">
            <h6>Shipping</h6>
            <p *ngIf="order.shippingName"><strong>Name:</strong> {{ order.shippingName }}</p>
            <p *ngIf="order.shippingAddress"><strong>Address:</strong> {{ order.shippingAddress }}</p>
            <p *ngIf="order.shippingPhone"><strong>Phone:</strong> {{ order.shippingPhone }}</p>
          </div>
          <div class="col-md-6 text-md-end mt-3 mt-md-0">
            <h6>Payment</h6>
            <p *ngIf="order.paymentTransactionId"><strong>Transaction:</strong> {{ order.paymentTransactionId }}</p>
            <p><strong>Total:</strong> {{ order.total | currency }}</p>
          </div>
        </div>

        <h5 class="mt-3">Items</h5>
        <mat-list *ngIf="order.items?.length>0">
          <mat-list-item *ngFor="let it of order.items">
            <div style="width:100%" class="d-flex justify-content-between align-items-center">
              <div>
                <div><strong>{{ it.productName || ('Product #' + it.productId) }}</strong></div>
                <div class="text-muted small">Qty: {{ it.quantity }} — Unit: {{ it.unitPrice | currency }}</div>
              </div>
              <div class="text-end"><strong>{{ (it.quantity * it.unitPrice) | currency }}</strong></div>
            </div>
          </mat-list-item>
        </mat-list>

      </mat-card-content>

      <mat-card-actions class="d-flex justify-content-between align-items-center mt-3">
        <div>
          <button *ngIf="order.status==='Processing'" class="btn btn-danger me-2" (click)="cancel()">Cancel order</button>
        </div>
        <div class="text-muted small">Order ID: {{ order.id }}</div>
      </mat-card-actions>
    </mat-card>

    <div *ngIf="!order && !loading" class="text-center mt-4">Order not found.</div>
    <div *ngIf="loading" class="text-center mt-4">Loading...</div>
  </div>
  `,
  styles: [
    `.order-detail-card { padding-bottom: 0; }
     mat-card-actions { padding: 12px 16px; }
     @media (max-width:767px) { .text-md-end { text-align: left !important; } }
    `
  ]
})
export class OrderDetailComponent {
  order: any = null;
  loading = false;

  constructor(private route: ActivatedRoute, private svc: OrderService, private router: Router, private notify: NotificationService) {
    this.load();
  }

  back() { this.router.navigate(['/orders']); }

  load() {
    this.loading = true;
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.svc.getOrderById(id).subscribe({ next: (res) => { this.order = res; this.loading = false; }, error: (err) => { this.loading = false; this.notify.error('Failed to load order'); } });
  }

  cancel() {
    if (!confirm('Cancel this order?')) return;
    this.svc.cancelOrder(this.order.id).subscribe({ next: () => { this.notify.success('Order cancelled'); this.load(); }, error: (err: any) => this.notify.error(err?.error?.message || 'Failed to cancel') });
  }
}
