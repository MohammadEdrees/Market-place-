import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from './http-params';
import { PagedResponse, ServiceInput, ServiceListing, ServiceQuery } from './models';

@Injectable({ providedIn: 'root' })
export class ServicesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/services';

  /** One page of services; filtering and sorting are applied by the API. */
  list(query: ServiceQuery = {}): Observable<PagedResponse<ServiceListing>> {
    const params = toParams({
      search: query.search,
      category: query.category,
      providerId: query.providerId,
      page: query.page,
      pageSize: query.pageSize,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
    return this.http.get<PagedResponse<ServiceListing>>(this.baseUrl, { params });
  }

  categories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

  /** The caller's own listings (small, unpaginated helper). */
  mine(): Observable<ServiceListing[]> {
    return this.http.get<ServiceListing[]>(`${this.baseUrl}/mine`);
  }

  create(input: ServiceInput): Observable<ServiceListing> {
    return this.http.post<ServiceListing>(this.baseUrl, input);
  }

  update(id: number, input: ServiceInput): Observable<ServiceListing> {
    return this.http.put<ServiceListing>(`${this.baseUrl}/${id}`, input);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
