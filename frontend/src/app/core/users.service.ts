import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { UserUpdateInput, UserProfile } from './models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/users';

  /** Profiles — admins see everyone, other roles only the provider directory. */
  list(): Observable<UserProfile[]> {
    return this.http.get<UserProfile[]>(this.baseUrl);
  }

  get(id: number): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/${id}`);
  }

  /** Updates the caller's own display name / contact fields. */
  updateMe(input: UserUpdateInput): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.baseUrl}/me`, input);
  }
}
