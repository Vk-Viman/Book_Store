import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private http: HttpClient) {}

  getProfile(): Observable<any> { return this.http.get('/api/users/me'); }
  updateProfile(payload: any) { return this.http.put('/api/users/me', payload); }
  changePassword(payload: any) { return this.http.put('/api/users/change-password', payload); }
}
