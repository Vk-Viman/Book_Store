import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { HttpClient } from '@angular/common/http';
import { LocalDatePipe } from '../../pipes/local-date.pipe';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatListModule, LocalDatePipe],
  template: `
  <div class="container mt-4">
    <button class="btn btn-link mb-2" (click)="back()">← Back to orders</button>
    <mat-card *ngIf="order">
      <mat-card-title>Order #{{ order.id }} — {{ order.orderDate | localDate:'medium' }}</mat-card-title>
      <mat-card-subtitle>
        Status: {{ order.orderStatus }} — Payment: {{ order.paymentStatus }} — Total: {{ order.totalAmount | currency }}
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
              <div><strong>{{ it.product?.title || ('Product #' + it.productId) }}</strong></div>
              <div class="text-muted">Qty: {{ it.quantity }} — Unit: {{ it.unitPrice | currency }} — Line: {{ (it.quantity * it.unitPrice) | currency }}</div>
            </div>
          </mat-list-item>
        </mat-list>

        <div class="mt-3" *ngIf="order.orderStatus==='Processing'">
          <button class="btn btn-danger" (click)="cancel()">Cancel order</button>
        </div>

        <div *ngIf="isDev" class="mt-3">
          <button class="btn btn-danger" (click)="deleteOrder()">Delete Order (dev only)</button>
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
  isDev = false;

  constructor(private route: ActivatedRoute, private cart: CartService, private router: Router, private http: HttpClient) {
    this.isDev = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
    this.load();
  }

  back() { this.router.navigate(['/orders']); }

  load() {
    this.loading = true;
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.cart.getOrders().subscribe({ next: (res: any) => { this.order = (res || []).find((o: any) => o.id === id) ?? null; this.loading = false; }, error: () => { this.loading = false; } });
  }

  cancel() {
    if (!confirm('Cancel this order?')) return;
    this.cart.cancelOrder(this.order.id).subscribe({ next: (res: any) => { alert('Order cancelled'); this.load(); }, error: (err: any) => alert(err?.error?.message || 'Failed to cancel') });
  }

  deleteOrder() {
    if (!confirm('Delete this order? This is a development-only action.')) return;
    // call backend dev endpoint to remove order
    const apiBase = (window as any)['__env']?.apiUrl ?? 'http://localhost:5005/api';
    this.http.delete(`${apiBase}/admin/orders/${this.order.id}`).subscribe({ next: () => { alert('Deleted'); this.router.navigate(['/orders']); }, error: () => alert('Failed to delete') });
  }
}
