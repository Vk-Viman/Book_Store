import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private loggedIn$ = new BehaviorSubject<boolean>(!!localStorage.getItem('token'));
  private role$ = new BehaviorSubject<string | null>(null);

  constructor(private http: HttpClient) {
    // Seed role from token if available (preferred) otherwise from localStorage
    const token = this.getToken();
    const roleFromToken = this.parseRoleFromToken(token);
    if (roleFromToken) {
      this.role$.next(roleFromToken);
      localStorage.setItem('role', roleFromToken);
    } else {
      const stored = localStorage.getItem('role');
      if (stored) this.role$.next(stored);
    }
    this.loggedIn$.next(!!token && !this.isTokenExpired(token));
  }

  register(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, data);
  }

  login(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, data);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getRefresh(): string | null {
    return localStorage.getItem('refresh');
  }

  private parseRoleFromToken(token?: string | null): string | null {
    const t = token ?? this.getToken();
    if (!t) return null;
    try {
      const payload = JSON.parse(atob(t.split('.')[1]));
      // common claim keys
      const keys = ['role', 'roles', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      for (const k of keys) {
        const v = payload[k];
        if (!v) continue;
        if (Array.isArray(v)) return v[0];
        return String(v);
      }
      return null;
    } catch {
      return null;
    }
  }

  setSession(token: string, refresh?: string, role?: string) {
    localStorage.setItem('token', token);
    if (refresh) localStorage.setItem('refresh', refresh);
    // prefer role param, otherwise parse from token
    let resolved = role ?? this.parseRoleFromToken(token) ?? null;
    if (resolved) {
      localStorage.setItem('role', resolved);
      this.role$.next(resolved);
    }
    this.loggedIn$.next(true);
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('refresh');
    localStorage.removeItem('role');
    this.role$.next(null);
    this.loggedIn$.next(false);
  }

  isTokenExpired(token?: string | null): boolean {
    const t = token ?? this.getToken();
    if (!t) return true;
    try {
      const payload = JSON.parse(atob(t.split('.')[1]));
      if (!payload.exp) return true;
      const now = Math.floor(Date.now() / 1000);
      return payload.exp < now;
    } catch {
      return true;
    }
  }

  isLoggedIn() { return this.loggedIn$.asObservable(); }

  isAdmin$() { return this.role$.asObservable().pipe(map(r => r === 'Admin')); }

  isAdmin(): boolean { return this.role$.value === 'Admin' || this.parseRoleFromToken(this.getToken()) === 'Admin' || localStorage.getItem('role') === 'Admin'; }

  refreshToken(): Observable<any> {
    const refresh = this.getRefresh();
    return this.http.post(`${this.apiUrl}/refresh`, refresh ?? '');
  }
}
