import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap, catchError, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class CartService {
  private _count = new BehaviorSubject<number>(0);
  cartCount$ = this._count.asObservable();

  constructor(private http: HttpClient) {
    // try to initialize count from server (silent)
    this.refreshCount();
  }

  getCart() {
    return this.http.get<any[]>('/api/cart').pipe(
      tap(items => this._count.next((items ?? []).length)),
      catchError(() => {
        // fallback to 0 on error
        this._count.next(0);
        return of([]);
      })
    );
  }

  addToCart(productId: number, quantity: number = 1) {
    return this.http.post(`/api/cart/${productId}?quantity=${quantity}`, {}).pipe(
      tap(() => this.refreshCount()),
      catchError(err => { throw err; })
    );
  }

  removeFromCart(productId: number) {
    return this.http.delete(`/api/cart/${productId}`).pipe(
      tap(() => this.refreshCount()),
      catchError(err => { throw err; })
    );
  }

  checkout() {
    return this.http.post('/api/orders/checkout', {}).pipe(
      tap(() => this.refreshCount()),
      catchError(err => { throw err; })
    );
  }

  getOrders() {
    return this.http.get<any[]>('/api/orders');
  }

  refreshCount() {
    this.http.get<any[]>('/api/cart').subscribe({ next: items => this._count.next((items ?? []).length), error: () => this._count.next(0) });
  }
}
