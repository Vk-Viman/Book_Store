import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap, catchError, of, Observable, Subject, throwError, from } from 'rxjs';
import { AuthService } from './auth.service';

const LOCAL_CART_KEY = 'readify_cart_v1';

interface LocalCartItem { productId: number; quantity: number; product?: any }

@Injectable({ providedIn: 'root' })
export class CartService {
  private _count = new BehaviorSubject<number>(0);
  cartCount$ = this._count.asObservable();

  // Notify when an order is successfully placed so UI can refresh product data
  private _orderCompleted = new Subject<void>();
  orderCompleted$ = this._orderCompleted.asObservable();

  private _wasLoggedIn = false;

  constructor(private http: HttpClient, private auth: AuthService) {
    // try to initialize count from server (silent) or local storage
    this.refreshCount();

    // merge local cart into server cart when user logs in
    this._wasLoggedIn = !!this.auth.getToken();
    this.auth.isLoggedIn().subscribe((logged: boolean) => {
      // if transitioning from logged-out -> logged-in, perform merge
      if (!this._wasLoggedIn && logged) {
        this.mergeLocalToServer();
      }
      this._wasLoggedIn = logged;
    });
  }

  private loadLocal(): LocalCartItem[] {
    try {
      const raw = localStorage.getItem(LOCAL_CART_KEY);
      if (!raw) return [];
      return JSON.parse(raw) as LocalCartItem[];
    } catch {
      return [];
    }
  }

  private saveLocal(items: LocalCartItem[]) {
    try {
      localStorage.setItem(LOCAL_CART_KEY, JSON.stringify(items));
      this._count.next(items.length);
    } catch { }
  }

  private mergeLocalToServer() {
    const items = this.loadLocal();
    if (!items || items.length === 0) return;

    const token = this.auth.getToken();
    if (!token) {
      // nothing to do for guests
      return;
    }

    // send all local items in one request to backend merge endpoint
    const payload = items.map(i => ({ productId: i.productId, quantity: i.quantity }));
    this.http.post('/api/cart/merge', payload).subscribe({
      next: () => {
        // clear local storage and refresh
        this.saveLocal([]);
        this.refreshCount();
      },
      error: () => {
        // on failure, keep local cart but still refresh count
        this.refreshCount();
      }
    });
  }

  getCart(): Observable<any[]> {
    const token = this.auth.getToken();
    if (!token) {
      const local = this.loadLocal();
      this._count.next(local.length);
      return from([local]);
    }

    return this.http.get<any[]>('/api/cart').pipe(
      tap((items: any[]) => this._count.next((items ?? []).length)),
      catchError(() => {
        // fallback to local
        const local = this.loadLocal();
        this._count.next(local.length);
        return from([local]);
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
      return from([{ ok: true }]);
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
      return from([null]);
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
          return from([null]);
        }
        existing.quantity = quantity;
        this.saveLocal(items);
        return from([existing]);
      }
      return from([null]);
    }
    return this.http.put('/api/cart/update', { productId, quantity }).pipe(
      tap(() => this.refreshCount()),
      catchError(err => { throw err; })
    );
  }

  checkout(shipping?: { shippingName?: string; shippingAddress?: string; shippingPhone?: string; promoCode?: string }): Observable<any> {
    const token = this.auth.getToken();
    if (!token) {
      // simulate order creation locally
      const items = this.loadLocal();
      if (!items || items.length === 0) return throwError(() => new Error('Cart is empty'));
      // clear local cart
      this.saveLocal([]);
      this.refreshCount();
      // notify order completed so UI can refresh product data
      this._orderCompleted.next();
      return from([{ ok: true, local: true }]);
    }

    // send shipping info as body to backend
    return this.http.post('/api/orders/checkout', shipping ?? {}).pipe(
      tap(() => { this.refreshCount(); this._orderCompleted.next(); }),
      catchError(err => { throw err; })
    );
  }

  getOrders(): Observable<any[]> {
    const token = this.auth.getToken();
    if (!token) return from([[]]);
    return this.http.get<any[]>('/api/orders');
  }

  refreshCount(): void {
    const token = this.auth.getToken();
    if (!token) {
      const local = this.loadLocal();
      this._count.next(local.length);
      return;
    }
    this.http.get<any[]>('/api/cart').subscribe({ next: (items: any[]) => this._count.next((items ?? []).length), error: () => this._count.next(0) });
  }
}
