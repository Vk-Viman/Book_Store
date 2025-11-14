import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RecommendationService {
  constructor(private http: HttpClient) {}

  getForMe(): Observable<any> {
    return this.http.get<any>('/api/recommendations/me');
  }

  refreshForMe(): Observable<any> {
    return this.http.post('/api/recommendations/refresh', {});
  }

  getPublic(): Observable<any> {
    return this.http.get<any>('/api/recommendations/public');
  }

  // admin refresh endpoint already wired in AdminDashboardService
}
