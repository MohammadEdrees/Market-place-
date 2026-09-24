import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from './http-params';
import { ListingImage, PagedResponse, Product, ProductInput, ProductQuery } from './models';

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

  /** Multipart upload of one or more gallery images (max 10 per listing, 5 MB each). */
  uploadImages(id: number, files: File[]): Observable<ListingImage[]> {
    const form = new FormData();
    for (const file of files) {
      form.append('files', file, file.name);
    }
    return this.http.post<ListingImage[]>(`${this.baseUrl}/${id}/images`, form);
  }

  /** Removes one image from the gallery and deletes the file on the server. */
  removeImage(id: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/images/${imageId}`);
  }
}
