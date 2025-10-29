import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap, catchError, of, Observable, Subject } from 'rxjs';
import { AuthService } from './auth.service';

const LOCAL_CART_KEY = 'readify_cart_v1';

@Injectable({ providedIn: 'root' })
export class CartService {
  private _count = new BehaviorSubject<number>(0);
  cartCount$ = this._count.asObservable();

  // Notify when an order is successfully placed so UI can refresh product data
  private _orderCompleted = new Subject<void>();
  orderCompleted$ = this._orderCompleted.asObservable();

  constructor(private http: HttpClient, private auth: AuthService) {
    // try to initialize count from server (silent) or local storage
    this.refreshCount();
  }

  private loadLocal(): any[] {
    try {
      const raw = localStorage.getItem(LOCAL_CART_KEY);
      if (!raw) return [];
      return JSON.parse(raw) as any[];
    } catch {
      return [];
    }
  }

  private saveLocal(items: any[]) {
    try {
      localStorage.setItem(LOCAL_CART_KEY, JSON.stringify(items));
      this._count.next(items.length);
    } catch { }
  }

  getCart(): Observable<any[]> {
    const token = this.auth.getToken();
    if (!token) {
      const local = this.loadLocal();
      this._count.next(local.length);
      return of(local);
    }

    return this.http.get<any[]>('/api/cart').pipe(
      tap(items => this._count.next((items ?? []).length)),
      catchError(() => {
        // fallback to local
        const local = this.loadLocal();
        this._count.next(local.length);
        return of(local);
      })
    );
  }

  addToCart(productId: number, quantity: number = 1, productObj?: any): Observable<any> {
    const token = this.auth.getToken();
    if (!token) {
      const items = this.loadLocal();
      const existing = items.find(i => i.productId === productId);
      if (existing) {
        existing.quantity += quantity;
      } else {
        items.push({ productId, quantity, product: productObj ?? null });
      }
      this.saveLocal(items);
      return of({ ok: true });
    }

    return this.http.post(`/api/cart/${productId}?quantity=${quantity}`, {}).pipe(
      tap(() => this.refreshCount()),
      catchError(err => { throw err; })
    );
  }

  removeFromCart(productId: number): Observable<any> {
    const token = this.auth.getToken();
    if (!token) {
      const items = this.loadLocal().filter(i => i.productId !== productId);
      this.saveLocal(items);
      return of(null);
    }
    return this.http.delete(`/api/cart/${productId}`).pipe(
      tap(() => this.refreshCount()),
      catchError(err => { throw err; })
    );
  }

  updateQuantity(productId: number, quantity: number): Observable<any> {
    const token = this.auth.getToken();
    if (!token) {
      const items = this.loadLocal();
      const existing = items.find(i => i.productId === productId);
      if (existing) {
        if (quantity <= 0) {
          const filtered = items.filter(i => i.productId !== productId);
          this.saveLocal(filtered);
          return of(null);
        }
        existing.quantity = quantity;
        this.saveLocal(items);
        return of(existing);
      }
      return of(null);
    }
    return this.http.put('/api/cart/update', { productId, quantity }).pipe(
      tap(() => this.refreshCount()),
      catchError(err => { throw err; })
    );
  }

  checkout(shipping?: { shippingName?: string; shippingAddress?: string; shippingPhone?: string }): Observable<any> {
    const token = this.auth.getToken();
    if (!token) {
      // simulate order creation locally
      const items = this.loadLocal();
      if (!items || items.length === 0) return of(new Error('Cart is empty'));
      // clear local cart
      this.saveLocal([]);
      this.refreshCount();
      // notify order completed so UI can refresh product data
      this._orderCompleted.next();
      return of({ ok: true, local: true });
    }

    // send shipping info as body to backend
    return this.http.post('/api/orders/checkout', shipping ?? {}).pipe(
      tap(() => { this.refreshCount(); this._orderCompleted.next(); }),
      catchError(err => { throw err; })
    );
  }

  getOrders(): Observable<any[]> {
    const token = this.auth.getToken();
    if (!token) return of([]);
    return this.http.get<any[]>('/api/orders');
  }

  refreshCount(): void {
    const token = this.auth.getToken();
    if (!token) {
      const local = this.loadLocal();
      this._count.next(local.length);
      return;
    }
    this.http.get<any[]>('/api/cart').subscribe({ next: items => this._count.next((items ?? []).length), error: () => this._count.next(0) });
  }
}
