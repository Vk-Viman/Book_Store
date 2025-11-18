import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { NotificationService } from '../../services/notification.service';
import { LocalDatePipe } from '../../pipes/local-date.pipe';
import { MatStepperModule } from '@angular/material/stepper';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatListModule, LocalDatePipe, MatStepperModule, MatButtonModule, MatIconModule],
  template: `
  <div class="container mt-4">
    <button class="btn btn-link mb-2" (click)="back()">← Back to orders</button>
    <mat-card *ngIf="order" class="order-detail-card">
      <mat-card-title>
        Order #{{ order.id }} — {{ (order.orderDate || order.createdAt) | localDate:'medium' }}
        <span *ngIf="order.promoCode" class="coupon-chip" title="Coupon applied">{{ order.promoCode }}</span>
      </mat-card-title>
      <mat-card-subtitle class="mb-2">Status: <strong>{{ displayStatus }}</strong></mat-card-subtitle>
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
            <div class="pricing">
              <div *ngIf="order.originalTotal != null" class="small">Subtotal: <span [class.text-decoration-line-through]="(order.discountAmount||0) > 0">{{ order.originalTotal | currency }}</span></div>
              <div *ngIf="(order.discountAmount||0) > 0" class="small text-success">Discount: -{{ order.discountAmount | currency }} <span *ngIf="order.discountPercent">({{ order.discountPercent }}%)</span></div>
              <div class="small">Shipping: <span *ngIf="order.freeShipping" class="text-success">Free</span><span *ngIf="!order.freeShipping">{{ order.shippingCost | currency }}</span></div>
              <p class="mt-1"><strong>Total:</strong> {{ (order.total ?? order.totalAmount) | currency }}</p>
            </div>
          </div>
        </div>

        <h5 class="mt-3">Items</h5>
        <mat-list *ngIf="order.items?.length>0">
          <mat-list-item *ngFor="let it of order.items">
            <div style="width:100%" class="d-flex justify-content-between align-items-center">
              <div>
                <div><strong>{{ it.product?.title || it.productName || ('Product #' + it.productId) }}</strong></div>
                <div class="text-muted small">Qty: {{ it.quantity }} — Unit: {{ (it.unitPrice || 0) | currency }}</div>
              </div>
              <div class="text-end"><strong>{{ (it.quantity * (it.unitPrice || 0)) | currency }}</strong></div>
            </div>
          </mat-list-item>
        </mat-list>

        <h5 class="mt-4">Order progress</h5>

        <mat-horizontal-stepper [selectedIndex]="selectedIndex" labelPosition="bottom" class="order-stepper">
          <mat-step *ngFor="let s of steps; let i = index" [completed]="i < selectedIndex">
            <ng-template matStepLabel>{{ s.label }}</ng-template>
            <div class="step-content">
              <div class="small text-muted">{{ s.timestamp ? (s.timestamp | localDate:'short') : '' }}</div>
            </div>
          </mat-step>
        </mat-horizontal-stepper>

      </mat-card-content>

      <mat-card-actions class="d-flex justify-content-between align-items-center mt-3">
        <div>
          <button *ngIf="displayStatus==='Processing'" class="btn btn-danger me-2" (click)="cancel()">Cancel order</button>
        </div>
        <div class="text-muted small">Order ID: {{ order.id }}</div>
      </mat-card-actions>
    </mat-card>

    <div *ngIf="!order && !loading" class="text-center mt-4">Order not found.</div>
    <div *ngIf="loading" class="text-center mt-4">Loading...</div>
  </div>
  `,
  styles: [`
    .order-detail-card { padding-bottom: 0; }
    mat-card-actions { padding: 12px 16px; }
    .order-stepper { margin-top: 12px; }
    .step-content { padding: 8px 0; }
    .coupon-chip { background:#e3f2fd; color:#0d47a1; border-radius:12px; padding:2px 8px; font-size:.8rem; margin-left:8px; }
    @media (max-width:767px) { .text-md-end { text-align: left !important; } }
  `]
})
export class OrderDetailComponent {
  order: any = null;
  loading = false;
  steps: any[] = [];
  selectedIndex = 0;
  displayStatus: string = '';

  constructor(private route: ActivatedRoute, private svc: OrderService, private router: Router, private notify: NotificationService) { this.load(); }

  back() { this.router.navigate(['/orders']); }

  load() {
    this.loading = true;
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.svc.getOrderById(id).subscribe({ next: (res) => { this.order = res; this.displayStatus = this.determineStatusFromOrder(res); this.loading = false; this.loadTimeline(id); }, error: (err) => { this.loading = false; this.notify.error('Failed to load order'); } });
  }

  loadTimeline(id: number) {
    this.svc.getOrderHistory(id).subscribe({ next: (res: any[]) => { this.buildTimeline(res); }, error: () => { this.steps = []; } });
  }

  private determineStatusFromOrder(o: any): string {
    // try multiple properties that may contain status
    if (!o) return '';
    return (o.status || o.orderStatus || o.orderStatusString || o.OrderStatusString || '').toString();
  }

  buildTimeline(events: any[]) {
    const base = ['Created','Pending','Processing','Shipped','Delivered'];
    const map: any = {};
    events.forEach(e => { if (e && e.newStatus) map[(e.newStatus || '').trim()] = e.timestamp; });

    // pick current status: prefer last history newStatus, otherwise fall back to order fields
    let current = '';
    try {
      if (events && events.length > 0) {
        const last = events[events.length - 1];
        current = (last?.newStatus || '').toString().trim();
      }
    } catch {}
    if (!current) {
      current = this.determineStatusFromOrder(this.order).toString().trim();
    }

    const newSteps = base.map(s => ({ label: s, timestamp: s === 'Created' ? (this.order?.createdAt ? new Date(this.order.createdAt) : null) : (map[s] ? new Date(map[s]) : null) }));

    let idx = newSteps.findIndex(st => (st.label || '').toLowerCase() === (current || '').toLowerCase());
    if (idx < 0) idx = 0;

    // assign and update selectedIndex in next tick to ensure stepper refresh
    this.steps = newSteps;
    setTimeout(() => { this.selectedIndex = idx; this.displayStatus = this.mapStatusLabel(current || this.displayStatus); }, 0);
  }

  private mapStatusLabel(s: string) {
    if (!s) return '';
    const normalized = s.toString().trim().toLowerCase();
    switch (normalized) {
      case 'pending': return 'Pending';
      case 'processing': return 'Processing';
      case 'shipped': return 'Shipped';
      case 'delivered': return 'Delivered';
      case 'created': return 'Created';
      default:
        // handle enum-like values e.g. OrderStatus.Delivered
        const parts = s.split('.');
        const candidate = parts[parts.length-1];
        return candidate.charAt(0).toUpperCase() + candidate.slice(1).toLowerCase();
    }
  }

  cancel() {
    if (!confirm('Cancel this order?')) return;
    this.svc.cancelOrder(this.order.id).subscribe({ next: () => { this.notify.success('Order cancelled'); this.load(); }, error: (err: any) => this.notify.error(err?.error?.message || 'Failed to cancel') });
  }
}
