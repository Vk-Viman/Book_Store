import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private loggedIn$ = new BehaviorSubject<boolean>(!!localStorage.getItem('token'));
  private role$ = new BehaviorSubject<string | null>(localStorage.getItem('role'));

  constructor(private http: HttpClient) {}

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

  setSession(token: string, refresh?: string, role?: string) {
    localStorage.setItem('token', token);
    if (refresh) localStorage.setItem('refresh', refresh);
    if (role) { localStorage.setItem('role', role); this.role$.next(role); }
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

  isAdmin(): boolean { return this.role$.value === 'Admin' || localStorage.getItem('role') === 'Admin'; }

  refreshToken(): Observable<any> {
    const refresh = this.getRefresh();
    return this.http.post(`${this.apiUrl}/refresh`, refresh ?? '');
  }
}
