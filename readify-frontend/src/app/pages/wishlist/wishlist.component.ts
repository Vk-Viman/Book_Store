import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WishlistService } from '../../services/wishlist.service';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { NotificationService } from '../../services/notification.service';
import { RouterLink } from '@angular/router';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule, MatIconModule, MatButtonModule, RouterLink],
  template: `
  <div class="container mt-4">
    <mat-card>
      <mat-card-title>My Wishlist</mat-card-title>
      <mat-card-content>
        <div *ngIf="items.length===0" class="text-center py-4">Your wishlist is empty.</div>
        <mat-list *ngIf="items.length>0">
          <mat-list-item *ngFor="let it of items">
            <div style="width:100%" class="d-flex justify-content-between align-items-center">
              <div class="d-flex align-items-center">
                <img [src]="it.product?.imageUrl || 'assets/book-placeholder.svg'" width="56" height="80" class="me-3" alt="{{it.product?.title}}" />
                <div>
                  <div><strong><a [routerLink]="['/books', it.product?.id]">{{ it.product?.title }}</a></strong></div>
                  <div class="text-muted small">{{ it.product?.price | currency }}</div>
                </div>
              </div>
              <div class="d-flex gap-2">
                <button mat-stroked-button color="primary" (click)="addToCart(it.product); $event.stopPropagation()">Add to Cart</button>
                <button mat-flat-button color="accent" (click)="moveToCart(it.product, it.productId); $event.stopPropagation()" [disabled]="moving.has(it.productId)">
                  <mat-icon *ngIf="!moving.has(it.productId)">arrow_forward</mat-icon>
                  <mat-icon *ngIf="moving.has(it.productId)">hourglass_top</mat-icon>
                  Move to Cart
                </button>
                <a mat-stroked-button color="primary" [routerLink]="['/books', it.product?.id]">View</a>
                <button mat-button color="warn" (click)="remove(it.productId)">Remove</button>
              </div>
            </div>
          </mat-list-item>
        </mat-list>
      </mat-card-content>
    </mat-card>
  </div>
  `
})
export class WishlistComponent {
  items: any[] = [];
  moving = new Set<number>();

  constructor(private svc: WishlistService, private notify: NotificationService, private cart: CartService) {
    this.load();
  }

  load() {
    this.svc.getMyWishlist().subscribe({ next: (res: any) => { this.items = res; }, error: () => { this.items = []; } });
  }

  remove(productId: number) {
    this.svc.removeFromWishlist(productId).subscribe({ next: () => { this.notify.success('Removed'); this.load(); }, error: () => this.notify.error('Failed to remove') });
  }

  addToCart(product: any) {
    if (!product) return;
    this.cart.addToCart(product.id).subscribe({ next: () => { this.notify.success('Added to cart'); }, error: () => { this.notify.error('Failed to add to cart'); } });
  }

  moveToCart(product: any, productId: number) {
    if (!product) return;
    if (this.moving.has(productId)) return;
    this.moving.add(productId);
    // add to cart, then remove from wishlist on success
    this.cart.addToCart(product.id).subscribe({
      next: () => {
        this.svc.removeFromWishlist(productId).subscribe({ next: () => { this.notify.success('Moved to cart'); this.moving.delete(productId); this.load(); }, error: () => { this.notify.error('Added to cart but failed to remove from wishlist'); this.moving.delete(productId); } });
      },
      error: () => { this.notify.error('Failed to add to cart'); this.moving.delete(productId); }
    });
  }
}
