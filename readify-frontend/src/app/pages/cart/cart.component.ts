import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartService } from '../../services/cart.service';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, MatListModule, MatButtonModule, MatCardModule, MatIconModule],
  template: `
  <div class="container mt-4">
    <mat-card>
      <mat-card-title>Shopping Cart</mat-card-title>
      <mat-card-content>
        <div *ngIf="loading" class="text-center py-4"><mat-icon>hourglass_empty</mat-icon> Loading...</div>
        <div *ngIf="!loading && items.length===0" class="text-center py-4">Your cart is empty.</div>
        <mat-list *ngIf="!loading && items.length>0">
          <mat-list-item *ngFor="let it of items">
            <img matListAvatar [src]="it.product?.imageUrl || 'assets/book-placeholder.svg'" alt="" width="56" />
            <h4 matLine>{{ it.product?.title }}</h4>
            <p matLine>{{ it.product?.price | currency }} x {{ it.quantity }} = {{ (it.product?.price * it.quantity) | currency }}</p>
            <button mat-icon-button color="warn" (click)="remove(it.productId)">
              <mat-icon>delete</mat-icon>
            </button>
          </mat-list-item>
        </mat-list>
      </mat-card-content>
      <mat-card-actions *ngIf="items.length>0" class="d-flex justify-content-between align-items-center">
        <div>
          <strong>Total: {{ total | currency }}</strong>
        </div>
        <div>
          <button mat-stroked-button color="primary" (click)="checkout()">Checkout</button>
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
    this.cart.getCart().subscribe({ next: (res: any) => { this.items = res; this.total = this.items.reduce((s: number, i: any) => s + ((i.product?.price ?? 0) * i.quantity), 0); this.loading = false; }, error: () => { this.loading = false; } });
  }

  remove(productId: number) {
    this.cart.removeFromCart(productId).subscribe({ next: () => { this.snack.open('Removed from cart', 'Close', { duration: 2000 }); this.load(); }, error: () => this.snack.open('Failed to remove', 'Close', { duration: 2000 }) });
  }

  checkout() {
    this.cart.checkout().subscribe({ next: (res: any) => { this.snack.open('Order placed', 'Close', { duration: 3000 }); this.items = []; this.total = 0; }, error: () => this.snack.open('Failed to checkout', 'Close', { duration: 3000 }) });
  }
}
