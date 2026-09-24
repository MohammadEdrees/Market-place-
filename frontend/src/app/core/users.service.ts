import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from './http-params';
import { PagedResponse, UserQuery, UserUpdateInput, UserProfile } from './models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/users';

  /**
   * One page of profiles — admins see everyone, other roles only the provider
   * directory. Pass `pageSize: 100` when you need the full list for name lookups.
   */
  list(query: UserQuery = {}): Observable<PagedResponse<UserProfile>> {
    const params = toParams({
      search: query.search,
      role: query.role,
      type: query.type,
      page: query.page,
      pageSize: query.pageSize,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
    return this.http.get<PagedResponse<UserProfile>>(this.baseUrl, { params });
  }

  get(id: number): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/${id}`);
  }

  /** Updates the caller's own display name / contact fields. */
  updateMe(input: UserUpdateInput): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.baseUrl}/me`, input);
  }
}
