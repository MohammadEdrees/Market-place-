import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { MarketOrder } from './models';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/orders';

  /** Orders visible to the caller (everything for dashboard admins). */
  list(): Observable<MarketOrder[]> {
    return this.http.get<MarketOrder[]>(this.baseUrl);
  }

  /** Orders the signed-in user placed. */
  mine(): Observable<MarketOrder[]> {
    return this.http.get<MarketOrder[]>(`${this.baseUrl}/mine`);
  }

  updateStatus(id: number, status: string): Observable<MarketOrder> {
    return this.http.put<MarketOrder>(`${this.baseUrl}/${id}/status`, { status });
  }
}
