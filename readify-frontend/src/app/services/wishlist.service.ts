import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class WishlistService {
  constructor(private http: HttpClient) {}

  getMyWishlist(): Observable<any[]> {
    return this.http.get<any[]>('/api/wishlist');
  }

  addToWishlist(productId: number): Observable<any> {
    return this.http.post(`/api/wishlist/${productId}`, {});
  }

  removeFromWishlist(productId: number): Observable<any> {
    return this.http.delete(`/api/wishlist/${productId}`);
  }
}
