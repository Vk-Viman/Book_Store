import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { lastValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private base = '/api';
  private imageCache = new Map<string, { ok: boolean; message?: string; ts: number }>();
  private cacheTtlMs = 1000 * 60 * 5; // 5 minutes

  constructor(private http: HttpClient) {}

  getProducts(options?: { q?: string; categoryId?: number; page?: number; pageSize?: number; sort?: string }): Observable<any> {
    let params = new HttpParams();
    if (options) {
      if (options.q) params = params.set('q', options.q);
      if (options.categoryId) params = params.set('categoryId', String(options.categoryId));
      if (options.page) params = params.set('page', String(options.page));
      if (options.pageSize) params = params.set('pageSize', String(options.pageSize));
      if (options.sort) params = params.set('sort', options.sort);
    }
    return this.http.get(`${this.base}/products`, { params });
  }

  getProduct(id: number): Observable<any> {
    return this.http.get(`${this.base}/products/${id}`);
  }

  getCategories(): Observable<any> {
    return this.http.get(`${this.base}/categories`);
  }

  createProduct(product: any): Observable<any> {
    return this.http.post(`${this.base}/admin/products`, product);
  }

  updateProduct(id: number, product: any): Observable<any> {
    return this.http.put(`${this.base}/admin/products/${id}`, product);
  }

  deleteProduct(id: number): Observable<any> {
    return this.http.delete(`${this.base}/admin/products/${id}`);
  }

  createCategory(name: string): Observable<any> {
    return this.http.post(`${this.base}/admin/categories`, { name });
  }

  // Validate image URL using backend endpoint with caching
  async validateImageUrl(url: string): Promise<{ ok: boolean; message?: string } > {
    if (!url) return { ok: true };
    const now = Date.now();
    const cached = this.imageCache.get(url);
    if (cached && (now - cached.ts) < this.cacheTtlMs) {
      return { ok: cached.ok, message: cached.message };
    }

    try {
      const resp: any = await lastValueFrom(this.http.post(`${this.base}/admin/image/validate`, { url }));
      const result = { ok: !!resp?.ok, message: resp?.message };
      this.imageCache.set(url, { ok: result.ok, message: result.message, ts: now });
      return result;
    } catch (err: any) {
      // cache negative result to avoid repeated failing requests for a short time
      const msg = err?.error?.message ?? 'Failed to validate image';
      this.imageCache.set(url, { ok: false, message: msg, ts: now });
      return { ok: false, message: msg };
    }
  }
}
