import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from './http-params';
import { AuditLog, AuditLogQuery, PagedResponse } from './models';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/auditlogs';

  /**
   * One page of the trail — newest first by default. Filtering, sorting and
   * paging all happen server-side, because this is the one list that keeps
   * growing (the API prunes past 10,000 entries).
   *
   * Admin callers only: every other role gets `403`, which is why the page
   * renders its "admins only" panel instead of calling this for Viewers.
   */
  list(query: AuditLogQuery = {}): Observable<PagedResponse<AuditLog>> {
    const params = toParams({
      search: query.search,
      action: query.action,
      entity: query.entity,
      userId: query.userId,
      page: query.page,
      pageSize: query.pageSize,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
    return this.http.get<PagedResponse<AuditLog>>(this.baseUrl, { params });
  }
}
