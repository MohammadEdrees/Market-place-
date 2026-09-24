import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from './http-params';
import { PagedResponse, UserCreateInput, UserQuery, UserUpdateInput, UserProfile } from './models';

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

  /** Admin creates an account (`SuperAdmin`/`Admin`/`Manager` only; others get 403). */
  create(input: UserCreateInput): Observable<UserProfile> {
    return this.http.post<UserProfile>(this.baseUrl, input);
  }

  /** Updates the caller's own display name / contact fields. */
  updateMe(input: UserUpdateInput): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.baseUrl}/me`, input);
  }

  /** Uploads (or replaces) the user's profile picture — account owner or dashboard admin. */
  uploadAvatar(id: number, file: File): Observable<UserProfile> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<UserProfile>(`${this.baseUrl}/${id}/image`, form);
  }

  /** Removes the profile picture and its file — account owner or dashboard admin. */
  removeAvatar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/image`);
  }
}
