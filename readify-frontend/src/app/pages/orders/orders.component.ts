import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartService } from '../../services/cart.service';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule],
  template: `
  <div class="container mt-4">
    <mat-card>
      <mat-card-title>Your Orders</mat-card-title>
      <mat-card-content>
        <div *ngIf="loading" class="text-center py-4">Loading orders...</div>
        <div *ngIf="!loading && orders.length===0" class="text-center py-4">You have no orders yet.</div>
        <mat-list *ngIf="!loading && orders.length>0">
          <mat-list-item *ngFor="let o of orders">
            <div>
              <h4 matLine>Order #{{ o.id }} - {{ o.orderDate | date:'medium' }}</h4>
              <p matLine>Total: {{ o.totalAmount | currency }} — Status: {{ o.status }}</p>
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
