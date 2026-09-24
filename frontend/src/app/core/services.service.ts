import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ServiceInput, ServiceListing } from './models';

@Injectable({ providedIn: 'root' })
export class ServicesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/services';

  list(search?: string, category?: string, providerId?: number): Observable<ServiceListing[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    if (category) params = params.set('category', category);
    if (providerId != null) params = params.set('providerId', String(providerId));
    return this.http.get<ServiceListing[]>(this.baseUrl, { params });
  }

  categories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

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
