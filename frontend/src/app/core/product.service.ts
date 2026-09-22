import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Product, ProductInput } from './models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  list(search?: string, category?: string): Observable<Product[]> {
    let params: Record<string, string> = {};
    if (search) params = { ...params, search };
    if (category) params = { ...params, category };
    return this.http.get<Product[]>(this.baseUrl, { params });
  }

  categories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

  create(input: ProductInput): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, input);
  }

  update(id: number, input: ProductInput): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, input);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
