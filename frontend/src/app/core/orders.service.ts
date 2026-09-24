import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from './http-params';
import { MarketOrder, OrderQuery, PagedResponse } from './models';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/orders';

  /** One page of the orders visible to the caller (everything for dashboard admins). */
  list(query: OrderQuery = {}): Observable<PagedResponse<MarketOrder>> {
    const params = toParams({
      search: query.search,
      kind: query.kind,
      status: query.status,
      page: query.page,
      pageSize: query.pageSize,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
    return this.http.get<PagedResponse<MarketOrder>>(this.baseUrl, { params });
  }

  /** Orders the signed-in user placed (small, unpaginated helper). */
  mine(): Observable<MarketOrder[]> {
    return this.http.get<MarketOrder[]>(`${this.baseUrl}/mine`);
  }

  updateStatus(id: number, status: string): Observable<MarketOrder> {
    return this.http.put<MarketOrder>(`${this.baseUrl}/${id}/status`, { status });
  }
}
