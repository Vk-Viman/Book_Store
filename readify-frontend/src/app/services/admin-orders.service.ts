import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AdminOrdersService {
  private base = '/api/admin/orders';
  constructor(private http: HttpClient) {}

  list(page = 1, pageSize = 50, status?: string, q?: string) {
    let params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    if (status) params = params.set('status', status);
    if (q) params = params.set('q', q);
    return this.http.get(this.base, { params });
  }

  updateStatus(id: number, payload: any) {
    return this.http.put(`${this.base}/update-status/${id}`, payload);
  }

  getById(id: number) { return this.http.get(`${this.base}/${id}`); }
}
