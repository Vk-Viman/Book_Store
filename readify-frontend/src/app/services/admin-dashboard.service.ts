import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AdminStatsDto {
  totalUsers: number;
  totalOrders: number;
  totalSales: number;
}

export interface TopProductDto {
  productName: string;
  quantitySold: number;
}

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  constructor(private http: HttpClient) {}

  getStats(): Observable<AdminStatsDto> {
    return this.http.get<AdminStatsDto>('/api/admin/stats');
  }

  getTopProducts(): Observable<TopProductDto[]> {
    // backend exposes top-products under the stats controller: /api/admin/stats/top-products
    return this.http.get<TopProductDto[]>('/api/admin/stats/top-products');
  }

  refreshRecommendations(): Observable<any> {
    return this.http.post('/api/admin/recommendations/refresh', {});
  }

  getRevenue(period: number = 30, from?: string, to?: string, categoryId?: number): Observable<any> {
    let params = new HttpParams().set('period', String(period));
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (categoryId) params = params.set('categoryId', String(categoryId));
    return this.http.get(`/api/admin/analytics/revenue`, { params });
  }

  getTopCategories(top: number = 10, from?: string, to?: string): Observable<any> {
    let params = new HttpParams().set('top', String(top));
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get(`/api/admin/analytics/top-categories`, { params });
  }

  getTopAuthors(top: number = 10, from?: string, to?: string): Observable<any> {
    let params = new HttpParams().set('top', String(top));
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get(`/api/admin/analytics/top-authors`, { params });
  }

  getUserTrend(period: number = 30, from?: string, to?: string): Observable<any> {
    let params = new HttpParams().set('period', String(period));
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get(`/api/admin/analytics/users`, { params });
  }

  getSummary(from?: string, to?: string, categoryId?: number): Observable<any> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (categoryId) params = params.set('categoryId', String(categoryId));
    return this.http.get(`/api/admin/analytics/summary`, { params });
  }

  refreshSummary(): Observable<any> {
    return this.http.post(`/api/admin/analytics/refresh`, {});
  }
}
