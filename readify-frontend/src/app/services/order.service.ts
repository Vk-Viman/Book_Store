import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OrderItemDto {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface OrderSummaryDto {
  id: number;
  createdAt: string; // ISO
  status: string;
  total: number;
  promoCode?: string; // optional coupon code
}

export interface OrderDetailDto extends OrderSummaryDto {
  items: OrderItemDto[];
  shippingName?: string;
  shippingAddress?: string;
  shippingPhone?: string;
  // Optional extended pricing fields returned by server
  originalTotal?: number;
  discountAmount?: number;
  discountPercent?: number;
  shippingCost?: number;
  freeShipping?: boolean;
  // Some endpoints may return camelCase server names
  totalAmount?: number;
  orderDate?: string;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  constructor(private http: HttpClient) {}

  getMyOrders(): Observable<OrderSummaryDto[]> {
    return this.http.get<OrderSummaryDto[]>('/api/orders/me');
  }

  getOrderById(id: number): Observable<OrderDetailDto> {
    return this.http.get<OrderDetailDto>(`/api/orders/${id}`);
  }

  getOrderHistory(id: number): Observable<any[]> {
    return this.http.get<any[]>(`/api/orders/${id}/history`);
  }

  checkout(payload: any): Observable<OrderDetailDto> {
    return this.http.post<OrderDetailDto>('/api/orders/checkout', payload);
  }

  cancelOrder(id: number): Observable<any> {
    return this.http.delete(`/api/orders/${id}`);
  }
}
