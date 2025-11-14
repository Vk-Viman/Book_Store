import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private base = '/api/reviews';
  private adminBase = '/api/admin/reviews';

  constructor(private http: HttpClient) {}

  postReview(payload: any): Observable<any> {
    return this.http.post(this.base, payload);
  }

  getApprovedForProduct(productId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/product/${productId}`);
  }

  // admin
  getPending(): Observable<any[]> {
    return this.http.get<any[]>(this.adminBase);
  }

  approve(id: number, approve: boolean): Observable<any> {
    return this.http.put(`${this.adminBase}/${id}/approve`, approve);
  }
}
