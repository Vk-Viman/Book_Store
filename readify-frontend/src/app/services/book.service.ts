import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BookService {
  private base = '/api/books';

  constructor(private http: HttpClient) {}

  getBooks(options?: { q?: string; categoryId?: number; categoryIds?: number[]; author?: string; page?: number; pageSize?: number; minPrice?: number | null; maxPrice?: number | null; sort?: string; minRating?: number | null; inStock?: boolean }): Observable<any> {
    let params = new HttpParams();
    if (options) {
      if (options.q) params = params.set('q', options.q);
      if (options.categoryId) params = params.set('categoryId', String(options.categoryId));
      if (options.categoryIds && options.categoryIds.length) {
        // add repeated params for arrays
        options.categoryIds.forEach(id => { params = params.append('categoryIds', String(id)); });
      }
      if (options.author) params = params.set('author', options.author);
      if (options.minPrice != null) params = params.set('minPrice', String(options.minPrice));
      if (options.maxPrice != null) params = params.set('maxPrice', String(options.maxPrice));
      if (options.sort) params = params.set('sort', options.sort);
      if (options.page) params = params.set('page', String(options.page));
      if (options.pageSize) params = params.set('pageSize', String(options.pageSize));
      if (options.minRating != null) params = params.set('minRating', String(options.minRating));
      if (options.inStock != null) params = params.set('inStock', options.inStock ? 'true' : 'false');
    }
    return this.http.get(this.base, { params });
  }

  getBook(id: number): Observable<any> {
    return this.http.get(`${this.base}/${id}`);
  }

  createBook(book: any): Observable<any> {
    return this.http.post(this.base, book);
  }

  updateBook(id: number, book: any): Observable<any> {
    return this.http.put(`${this.base}/${id}`, book);
  }

  deleteBook(id: number): Observable<any> {
    return this.http.delete(`${this.base}/${id}`);
  }
}
