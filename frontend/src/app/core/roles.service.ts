import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Role, RoleInput } from './models';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/roles';

  /** Every role, ordered by id (`userCount` is the accounts currently assigned). */
  list(): Observable<Role[]> {
    return this.http.get<Role[]>(this.baseUrl);
  }

  get(id: number): Observable<Role> {
    return this.http.get<Role>(`${this.baseUrl}/${id}`);
  }

  /** Admin creates a role (`SuperAdmin`/`Admin`/`Manager` only; 409 on a duplicate name). */
  create(input: RoleInput): Observable<Role> {
    return this.http.post<Role>(this.baseUrl, input);
  }

  /** Admin renames/redescribes a role — 409 when another role uses the name. */
  update(id: number, input: RoleInput): Observable<Role> {
    return this.http.put<Role>(`${this.baseUrl}/${id}`, input);
  }

  /** Admin deletes a role — 409 while accounts still use it. */
  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
