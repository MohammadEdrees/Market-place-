import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from './http-params';
import { Category, CategoryInput, CategoryKind, CategoryRenameInput } from './models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/categories';

  /** Managed categories (Product-kind first, then name A→Z); `kind` narrows the list. */
  list(kind?: CategoryKind): Observable<Category[]> {
    return this.http.get<Category[]>(this.baseUrl, { params: toParams({ kind }) });
  }

  get(id: number): Observable<Category> {
    return this.http.get<Category>(`${this.baseUrl}/${id}`);
  }

  /** Admin creates a category — 400 on an empty name, 409 on a duplicate (case-insensitive per kind). */
  create(input: CategoryInput): Observable<Category> {
    return this.http.post<Category>(this.baseUrl, input);
  }

  /** Admin renames a category (existing listings are renamed server-side) — 409 on a duplicate. */
  update(id: number, input: CategoryRenameInput): Observable<Category> {
    return this.http.put<Category>(`${this.baseUrl}/${id}`, input);
  }

  /** Admin deletes a category — 409 while products/services still use it. */
  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
