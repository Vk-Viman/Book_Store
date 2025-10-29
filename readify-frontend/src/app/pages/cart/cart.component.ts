import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartService } from '../../services/cart.service';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CartItemComponent } from '../../components/cart-item.component';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, MatListModule, MatButtonModule, MatCardModule, MatIconModule, CartItemComponent],
  template: `
  <div class="container mt-4">
    <mat-card>
      <mat-card-title>Shopping Cart</mat-card-title>
      <mat-card-content>
        <div *ngIf="loading" class="text-center py-4"><mat-icon>hourglass_empty</mat-icon> Loading...</div>
        <div *ngIf="!loading && items.length===0" class="text-center py-4">Your cart is empty.</div>
        <mat-list *ngIf="!loading && items.length>0">
          <mat-list-item *ngFor="let it of items">
            <app-cart-item [item]="it" (quantityChange)="changeQty($event.productId, $event.quantity)" (removeItem)="remove($event)"></app-cart-item>
          </mat-list-item>
        </mat-list>
      </mat-card-content>
      <mat-card-actions *ngIf="items.length>0" class="d-flex justify-content-between align-items-center">
        <div>
          <strong>Total: {{ total | currency }}</strong>
        </div>
        <div>
          <a class="btn btn-outline-primary" routerLink="/checkout">Checkout</a>
        </div>
      </mat-card-actions>
    </mat-card>
  </div>
  `
})
export class CartComponent {
  items: any[] = [];
  loading = false;
  total = 0;

  constructor(private cart: CartService, private snack: MatSnackBar) {
    this.load();
  }

  load() {
    this.loading = true;
    this.cart.getCart().subscribe(
      (res: any) => { this.items = res; this.total = this.items.reduce((s: number, i: any) => s + ((i.product?.price ?? 0) * i.quantity), 0); this.loading = false; },
      () => { this.loading = false; }
    );
  }

  changeQty(productId: number, qty: number) {
    this.cart.updateQuantity(productId, qty).subscribe(
      () => { this.load(); },
      (err: any) => { const msg = err?.error?.message || err?.message || 'Failed to update quantity'; this.snack.open(msg, 'Close', { duration: 3000 }); }
    );
  }

  remove(productId: number) {
    this.cart.removeFromCart(productId).subscribe(
      () => { this.snack.open('Removed from cart', 'Close', { duration: 2000 }); this.load(); },
      (err: any) => {
        const msg = err?.error?.message || err?.message || 'Failed to remove';
        this.snack.open(msg, 'Close', { duration: 2000 });
      }
    );
  }

  checkout() {
    this.cart.checkout().subscribe(
      (res: any) => { this.snack.open('Order placed', 'Close', { duration: 3000 }); this.items = []; this.total = 0; },
      (err: any) => {
        const msg = err?.error?.message || err?.message || 'Failed to checkout';
        this.snack.open(msg, 'Close', { duration: 3000 });
      }
    );
  }
}
