import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrderService, OrderSummaryDto } from '../../services/order.service';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { RouterModule } from '@angular/router';
import { LoadingSkeletonComponent } from '../../components/loading-skeleton.component';
import { LocalDatePipe } from '../../pipes/local-date.pipe';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule, RouterModule, LoadingSkeletonComponent, LocalDatePipe],
  styles: [
    `.order-title { font-weight: 600; font-size: 1rem; margin: 0; }
     .order-meta { font-size: 0.95rem; color: rgba(0,0,0,0.75); margin: 0; }
     mat-list-item { align-items: start; }
     mat-card { overflow: visible; padding: 16px; }
     .order-link { text-decoration: none; color: inherit; display: block; padding: 8px 0; }
     .orders-card { max-width: 900px; }
     .coupon-badge { background:#e3f2fd; color:#0d47a1; border-radius:12px; padding:2px 8px; font-size:.8rem; margin-left:8px; }
     @media (max-width: 767px) {
       mat-list-item { display: block; }
       .order-link { display: block; padding: 12px; border-radius: 8px; background: #fff; box-shadow: 0 1px 2px rgba(0,0,0,0.04); margin-bottom: 8px; }
     }
    `
  ],
  template: `
  <div class="container mt-4">
    <mat-card class="orders-card">
      <mat-card-title>Your Orders</mat-card-title>
      <mat-card-content>
        <div *ngIf="loading" class="py-4" aria-busy="true"><app-loading-skeleton type="list"></app-loading-skeleton></div>
        <div *ngIf="!loading && orders.length===0" class="text-center py-4" role="status">You have no orders yet. Place an order to see it here.</div>
        <mat-list *ngIf="!loading && orders.length>0">
          <mat-list-item *ngFor="let o of orders">
            <a [routerLink]="['/orders', o.id]" class="order-link" attr.aria-label="Open order {{o.id}} details">
              <div style="width:100%" class="d-flex justify-content-between align-items-center">
                <div>
                  <div class="order-title">Order #{{ o.id }} <span *ngIf="o.promoCode" class="coupon-badge" title="Coupon applied">{{ o.promoCode }}</span></div>
                  <div class="order-meta">{{ o.createdAt | localDate:'medium' }}</div>
                </div>
                <div class="text-end">
                  <div class="order-meta">{{ o.status }}</div>
                  <div class="order-title">{{ o.total | currency }}</div>
                </div>
              </div>
            </a>
          </mat-list-item>
        </mat-list>
      </mat-card-content>
    </mat-card>
  </div>
  `
})
export class OrdersComponent {
  orders: OrderSummaryDto[] = [];
  loading = false;

  constructor(private svc: OrderService, private notify: NotificationService) { this.load(); }

  load() { this.loading = true; this.svc.getMyOrders().subscribe({ next: (res) => { this.orders = res; this.loading = false; }, error: () => { this.loading = false; this.notify.error('Failed to load orders'); } }); }
}
