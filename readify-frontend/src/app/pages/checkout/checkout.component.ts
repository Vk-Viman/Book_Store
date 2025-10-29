import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../services/cart.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="container mt-4">
    <h2>Checkout</h2>
    <form #f="ngForm" (ngSubmit)="submit()">
      <div class="mb-3">
        <label class="form-label">Full name</label>
        <input name="name" [(ngModel)]="model.name" required class="form-control" />
      </div>
      <div class="mb-3">
        <label class="form-label">Address</label>
        <textarea name="address" [(ngModel)]="model.address" required class="form-control"></textarea>
      </div>
      <div class="mb-3">
        <label class="form-label">Phone</label>
        <input name="phone" [(ngModel)]="model.phone" required class="form-control" />
      </div>
      <button class="btn btn-primary" [disabled]="processing">Pay (Mock)</button>
    </form>
  </div>
  `
})
export class CheckoutComponent {
  model = { name: '', address: '', phone: '' };
  processing = false;

  constructor(private cart: CartService, private router: Router) {}

  submit() {
    if (!this.model.name || !this.model.address || !this.model.phone) return;
    this.processing = true;
    // For now we send checkout and then redirect to orders
    this.cart.checkout().subscribe({ next: () => { this.processing = false; this.router.navigate(['/orders']); }, error: () => { this.processing = false; alert('Checkout failed'); } });
  }
}
