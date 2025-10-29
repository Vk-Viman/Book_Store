import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-cart-item',
  standalone: true,
  template: `
    <div class="cart-item d-flex align-items-center justify-content-between">
      <div class="d-flex align-items-center">
        <img [src]="item.product?.imageUrl || 'assets/book-placeholder.svg'" width="60" class="me-3" />
        <div>
          <div><strong>{{ item.product?.title }}</strong></div>
          <div class="text-muted small">{{ item.product?.authors }}</div>
        </div>
      </div>
      <div class="d-flex align-items-center gap-2">
        <button class="btn btn-sm btn-outline-secondary" (click)="dec()">-</button>
        <input type="number" class="form-control form-control-sm" style="width:60px" [value]="item.quantity" (change)="onInput($event)" />
        <button class="btn btn-sm btn-outline-secondary" (click)="inc()">+</button>
        <button class="btn btn-sm btn-danger" (click)="remove()">Remove</button>
      </div>
    </div>
  `
})
export class CartItemComponent {
  @Input() item: any;
  @Output() quantityChange = new EventEmitter<{ productId: number, quantity: number }>();
  @Output() removeItem = new EventEmitter<number>();

  inc() { this.quantityChange.emit({ productId: this.item.productId, quantity: this.item.quantity + 1 }); }
  dec() { this.quantityChange.emit({ productId: this.item.productId, quantity: Math.max(0, this.item.quantity - 1) }); }
  remove() { this.removeItem.emit(this.item.productId); }
  onInput(e: any) { const val = Number(e.target.value || 0); this.quantityChange.emit({ productId: this.item.productId, quantity: Math.max(0, val) }); }
}
