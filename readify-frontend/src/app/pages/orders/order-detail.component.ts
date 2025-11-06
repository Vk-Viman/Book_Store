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
    <mat-card *ngIf="order">
      <mat-card-title>Order #{{ order.id }} — {{ order.createdAt | localDate:'medium' }}</mat-card-title>
      <mat-card-subtitle>
        Status: {{ order.status }} — Total: {{ order.total | currency }}
      </mat-card-subtitle>
      <mat-card-content class="mt-2">
        <h5>Shipping</h5>
        <p *ngIf="order.shippingName"><strong>Name:</strong> {{ order.shippingName }}</p>
        <p *ngIf="order.shippingAddress"><strong>Address:</strong> {{ order.shippingAddress }}</p>
        <p *ngIf="order.shippingPhone"><strong>Phone:</strong> {{ order.shippingPhone }}</p>

        <h5 class="mt-3">Items</h5>
        <mat-list *ngIf="order.items?.length>0">
          <mat-list-item *ngFor="let it of order.items">
            <div style="width:100%">
              <div><strong>{{ it.productName || ('Product #' + it.productId) }}</strong></div>
              <div class="text-muted">Qty: {{ it.quantity }} — Unit: {{ it.unitPrice | currency }} — Line: {{ (it.quantity * it.unitPrice) | currency }}</div>
            </div>
          </mat-list-item>
        </mat-list>

        <div class="mt-3" *ngIf="order.status==='Processing'">
          <button class="btn btn-danger" (click)="cancel()">Cancel order</button>
        </div>

      </mat-card-content>
    </mat-card>

    <div *ngIf="!order && !loading" class="text-center mt-4">Order not found.</div>
    <div *ngIf="loading" class="text-center mt-4">Loading...</div>
  </div>
  `
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
