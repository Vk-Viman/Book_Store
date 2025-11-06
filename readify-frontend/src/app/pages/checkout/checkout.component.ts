import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../services/cart.service';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { LoadingSkeletonComponent } from '../../components/loading-skeleton.component';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSkeletonComponent],
  template: `
  <div class="container mt-4">
    <h2>Checkout</h2>
    <form #f="ngForm" (ngSubmit)="submit()" aria-label="Checkout form">
      <div class="row">
        <div class="col-md-6">
          <div *ngIf="loading" aria-busy="true"><app-loading-skeleton type="text" [count]="3"></app-loading-skeleton></div>

          <div class="mb-3">
            <label class="form-label">Full name</label>
            <input name="name" [(ngModel)]="model.name" required class="form-control" aria-label="Full name" />
          </div>
          <div class="mb-3">
            <label class="form-label">Address</label>
            <textarea name="address" [(ngModel)]="model.address" required class="form-control" aria-label="Address"></textarea>
          </div>
          <div class="mb-3">
            <label class="form-label">Phone</label>
            <input name="phone" [(ngModel)]="model.phone" required class="form-control" aria-label="Phone" />
          </div>

          <div class="mb-3">
            <label class="form-label">Shipping region</label>
            <select class="form-select" [(ngModel)]="selectedRegion" name="region" (change)="onRegionChange()" aria-label="Shipping region">
              <option *ngFor="let r of shippingRegions" [value]="r.key">{{ r.label }}</option>
            </select>
            <div class="form-text">Shipping rate will be computed server-side for the selected region.</div>
          </div>

          <div class="mb-3">
            <label class="form-label">Promo code (optional)</label>
            <div class="input-group">
              <input name="promo" [(ngModel)]="promoCode" class="form-control" placeholder="Enter promo code" aria-label="Promo code" />
              <button type="button" class="btn btn-outline-secondary" (click)="validatePromo()" [disabled]="validatingPromo">Apply</button>
            </div>
            <div *ngIf="promoValid" class="text-success small mt-1">Valid promo: {{ promoValidMessage }}</div>
            <div *ngIf="promoError" class="text-danger small mt-1">{{ promoError }}</div>
          </div>
        </div>

        <div class="col-md-6">
          <div class="card summary-card p-3" aria-live="polite">
            <h5>Order summary</h5>
            <div *ngIf="items?.length === 0" class="text-muted">Your cart is empty.</div>

            <div *ngFor="let it of items; trackBy: trackByProduct" class="d-flex justify-content-between align-items-center py-2 border-bottom">
              <div>
                <div><strong>{{ it.product?.title || 'Item' }}</strong></div>
                <div class="text-muted small">{{ it.product?.authors }}</div>
              </div>
              <div class="d-flex align-items-center gap-2">
                <div class="text-end" style="min-width:120px;">
                  <div>{{ it.quantity }} × {{ formatCurrency(it.product?.price ?? it.unitPrice ?? 0) }}</div>
                  <div><strong>{{ formatCurrency((it.product?.price ?? it.unitPrice ?? 0) * it.quantity) }}</strong></div>
                </div>
              </div>
            </div>

            <div class="mt-2 d-flex justify-content-between"><div>Subtotal</div><div>{{ formatCurrency(rawTotal) }}</div></div>
            <div class="d-flex justify-content-between" *ngIf="discountAmount > 0"><div>Discount {{ promoValidMessage }}</div><div>-{{ formatCurrency(discountAmount) }}</div></div>
            <div class="d-flex justify-content-between" *ngIf="promoType === 'FreeShipping'"><div>Shipping</div><div class="text-success">Free</div></div>
            <div class="d-flex justify-content-between" *ngIf="promoType !== 'FreeShipping'"><div>Shipping</div><div>{{ formatCurrency(shippingRate) }}</div></div>
            <hr />
            <div class="d-flex justify-content-between"><div><strong>Total</strong></div><div><strong>{{ formatCurrency(discountedTotal) }}</strong></div></div>

            <div class="mt-3 text-end">
              <button class="btn btn-primary" [disabled]="processing">Pay (Mock)</button>
            </div>
          </div>
        </div>
      </div>
    </form>
  </div>
  `,
  styles: [
    `.summary-card { background: #fff; border-radius: 8px; box-shadow: 0 1px 4px rgba(0,0,0,0.04); }
     @media (max-width:767px) { .summary-card { margin-top: 16px; } }
    `
  ]
})
export class CheckoutComponent implements OnInit {
  model = { name: '', address: '', phone: '' };
  processing = false;

  shippingRegions = [
    { key: 'local', label: 'Local' },
    { key: 'national', label: 'National' },
    { key: 'international', label: 'International' }
  ];
  selectedRegion = 'national';
  shippingRate = 0;

  promoCode: string = '';
  validatingPromo = false;
  promoValid = false;
  promoValidMessage = '';
  promoError = '';

  promoType: string | null = null;
  promoFixedAmount: number | null = null;

  rawTotal = 0;
  discountedTotal = 0;
  discountAmount = 0;

  items: any[] = [];
  loading = false;

  constructor(private cart: CartService, private router: Router, private http: HttpClient) {}

  ngOnInit(): void {
    this.onRegionChange();
    this.loadCartTotal();
  }

  onRegionChange() {
    this.http.get<any>(`/api/shipping/rate?region=${encodeURIComponent(this.selectedRegion)}&subtotal=${this.rawTotal}`).subscribe({ next: res => { this.shippingRate = res?.rate ?? 0; this.computeTotals(); }, error: () => { this.shippingRate = 0; this.computeTotals(); } });
  }

  private loadCartTotal() {
    this.loading = true;
    this.cart.getCart().subscribe({ next: (items: any[]) => {
        this.items = items ?? [];
        this.rawTotal = this.items.reduce((s: number, i: any) => s + ((i.product?.price ?? i.unitPrice ?? 0) * i.quantity), 0);
        // refresh shipping rate because subtotal changed
        this.onRegionChange();
        this.loading = false;
      }, error: () => { this.items = []; this.rawTotal = 0; this.computeTotals(); this.loading = false; } });
  }

  changeQty(productId: number, qty: number) {
    qty = Math.max(0, Math.floor(qty));
    this.cart.updateQuantity(productId, qty).subscribe({ next: () => { this.loadCartTotal(); }, error: (err: any) => { /* ignore or show notification */ } });
  }

  onInputChange(productId: number, event: any) {
    const val = Number(event.target.value || 0);
    this.changeQty(productId, val);
  }

  private computeTotals() {
    const shipping = this.promoType === 'FreeShipping' ? 0 : this.shippingRate;

    // default
    this.discountAmount = 0;

    if (this.promoValid && this.promoType) {
      if (this.promoType === 'Percentage') {
        const match = this.promoValidMessage.match(/([0-9]+(?:\.[0-9]+)?)/);
        const pct = Number(match?.[1] ?? 0);
        const discount = Math.round((this.rawTotal * pct) / 100 * 100) / 100;
        this.discountAmount = discount;
      } else if (this.promoType === 'Fixed') {
        this.discountAmount = this.promoFixedAmount ?? 0;
      } else if (this.promoType === 'FreeShipping') {
        this.discountAmount = 0;
      }
    }

    // final total = subtotal + shipping - discount (clamped >= 0)
    this.discountedTotal = Math.max(0, this.rawTotal + shipping - (this.discountAmount ?? 0));
  }

  formatCurrency(value: number) {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value ?? 0);
  }

  validatePromo() {
    this.promoError = '';
    this.promoValid = false;
    this.promoType = null;
    this.promoFixedAmount = null;
    const code = (this.promoCode || '').trim();
    if (!code) { this.promoError = 'Please enter a promo code'; return; }
    this.validatingPromo = true;
    this.http.get<any>(`/api/cart/promo/${encodeURIComponent(code)}`).subscribe({
      next: (res: any) => {
        this.validatingPromo = false;
        this.promoValid = true;
        this.promoType = res?.type ?? 'Percentage';
        this.promoFixedAmount = res?.fixedAmount ?? null;
        if (this.promoType === 'Percentage') {
          this.promoValidMessage = res?.discountPercent ? `${res.discountPercent}% off` : 'Promo applied';
        } else if (this.promoType === 'Fixed') {
          this.promoValidMessage = res?.fixedAmount ? `${res.fixedAmount} off` : 'Promo applied';
        } else if (this.promoType === 'FreeShipping') {
          this.promoValidMessage = 'Free shipping';
        }
        this.computeTotals();
      },
      error: (err: any) => {
        this.validatingPromo = false;
        this.promoError = err?.error?.message || 'Invalid promo code';
        this.computeTotals();
      }
    });
  }

  submit() {
    if (!this.model.name || !this.model.address || !this.model.phone) return;
    this.processing = true;
    const body: any = { shippingName: this.model.name, shippingAddress: this.model.address, shippingPhone: this.model.phone };
    if (this.promoValid && this.promoCode) body.promoCode = this.promoCode.trim();
    body.region = this.selectedRegion;

    this.cart.checkout(body).subscribe({ next: () => { this.processing = false; this.router.navigate(['/orders']); }, error: (err: any) => {
        this.processing = false;
        const serverMessage = err?.error?.message;
        const serverDetail = err?.error?.error;
        alert((serverMessage ? serverMessage : 'Checkout failed') + (serverDetail ? '\n\n' + serverDetail : ''));
      } });
  }

  trackByProduct(index: number, item: any) { return item?.productId ?? item?.product?.id ?? index; }
}
