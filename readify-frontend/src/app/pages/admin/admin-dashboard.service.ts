import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
}
