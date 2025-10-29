import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatListModule],
  template: `
  <div class="container mt-4">
    <button class="btn btn-link mb-2" routerLink="/orders">← Back to orders</button>
    <mat-card *ngIf="order">
      <mat-card-title>Order #{{ order.id }} — {{ order.orderDate | date:'medium' }}</mat-card-title>
      <mat-card-subtitle>Status: {{ order.status }} — Total: {{ order.totalAmount | currency }}</mat-card-subtitle>
      <mat-card-content class="mt-2">
        <h5>Shipping</h5>
        <p *ngIf="order.shippingName"><strong>Name:</strong> {{ order.shippingName }}</p>
        <p *ngIf="order.shippingAddress"><strong>Address:</strong> {{ order.shippingAddress }}</p>
        <p *ngIf="order.shippingPhone"><strong>Phone:</strong> {{ order.shippingPhone }}</p>

        <h5 class="mt-3">Items</h5>
        <mat-list *ngIf="order.items?.length>0">
          <mat-list-item *ngFor="let it of order.items">
            <div style="width:100%">
              <div><strong>{{ it.product?.title || ('Product #' + it.productId) }}</strong></div>
              <div class="text-muted">Qty: {{ it.quantity }} — Unit: {{ it.unitPrice | currency }} — Line: {{ (it.quantity * it.unitPrice) | currency }}</div>
            </div>
          </mat-list-item>
        </mat-list>
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

  constructor(private route: ActivatedRoute, private cart: CartService) {
    this.load();
  }

  load() {
    this.loading = true;
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.cart.getOrders().subscribe({ next: (res: any) => { this.order = (res || []).find((o: any) => o.id === id) ?? null; this.loading = false; }, error: () => { this.loading = false; } });
  }
}
