import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartService } from '../../services/cart.service';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule],
  styles: [
    `.order-title { font-weight: 600; font-size: 1rem; margin: 0; }
     .order-meta { font-size: 0.95rem; color: rgba(0,0,0,0.75); margin: 0; }
     mat-list-item { align-items: start; }
     mat-card { overflow: visible; }
    `
  ],
  template: `
  <div class="container mt-4">
    <mat-card>
      <mat-card-title>Your Orders</mat-card-title>
      <mat-card-content>
        <div *ngIf="loading" class="text-center py-4">Loading orders...</div>
        <div *ngIf="!loading && orders.length===0" class="text-center py-4">You have no orders yet.</div>
        <mat-list *ngIf="!loading && orders.length>0">
          <mat-list-item *ngFor="let o of orders">
            <div style="width:100%">
              <div matLine class="order-title">Order #{{ o.id }} - {{ o.orderDate | date:'medium' }}</div>
              <div matLine class="order-meta">Total: {{ o.totalAmount | currency }} — Status: {{ o.status }}</div>
            </div>
          </mat-list-item>
        </mat-list>
      </mat-card-content>
    </mat-card>
  </div>
  `
})
export class OrdersComponent {
  orders: any[] = [];
  loading = false;

  constructor(private cart: CartService) { this.load(); }

  load() { this.loading = true; this.cart.getOrders().subscribe({ next: (res: any) => { this.orders = res; this.loading = false; }, error: () => { this.loading = false; } }); }
}
