import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { DashboardResponse, TrendPoint } from './models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  /** Full dashboard payload: metrics, charts, recent orders. */
  getDashboard(): Observable<DashboardResponse> {
    return this.http.get<DashboardResponse>('/api/dashboard');
  }

  /** Revenue + order count for the last 12 months. */
  getRevenueTrend(): Observable<TrendPoint[]> {
    return this.http.get<TrendPoint[]>('/api/dashboard/revenue-trend');
  }
}
