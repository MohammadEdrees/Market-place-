import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from './http-params';
import { PagedResponse, Product, ProductInput, ProductQuery } from './models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  /** One page of products; filtering and sorting are applied by the API. */
  list(query: ProductQuery = {}): Observable<PagedResponse<Product>> {
    const params = toParams({
      search: query.search,
      category: query.category,
      sellerId: query.sellerId,
      page: query.page,
      pageSize: query.pageSize,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
    return this.http.get<PagedResponse<Product>>(this.baseUrl, { params });
  }

  categories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

  /** The caller's own listings (small, unpaginated helper). */
  mine(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/mine`);
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
