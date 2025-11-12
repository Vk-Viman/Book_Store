import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AdminUsersService {
  private base = '/api/admin/users';
  constructor(private http: HttpClient) {}

  list(page = 1, pageSize = 20, q?: string): Observable<any> {
    let params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    if (q) params = params.set('q', q);
    return this.http.get(this.base, { params });
  }

  getById(id: number) {
    return this.http.get(`${this.base}/${id}`);
  }

  update(id: number, payload: any) {
    return this.http.put(`${this.base}/${id}`, payload);
  }

  toggleActive(id: number) {
    return this.http.put(`${this.base}/${id}/toggle-active`, {});
  }

  promote(id: number) {
    return this.http.put(`${this.base}/${id}/promote`, {});
  }

  delete(id: number) {
    return this.http.delete(`${this.base}/${id}`);
  }
}
