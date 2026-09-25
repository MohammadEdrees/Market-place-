import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { BackupFile, RestoreReport } from './models';

/** Restore strategy: upsert by id without deleting, or wipe every table first. */
export type RestoreMode = 'merge' | 'replace';

/** Backup & restore endpoints backing the dashboard's Settings page (all admin-only). */
@Injectable({ providedIn: 'root' })
export class BackupService {
  private readonly http = inject(HttpClient);

  /** Stored snapshots with their row counts, newest first. */
  list(): Observable<BackupFile[]> {
    return this.http.get<BackupFile[]>('/api/backup');
  }

  /**
   * Snapshots the live data server-side and streams the JSON file back, so one click
   * both downloads a copy and records it in the server-side history.
   */
  export(): Observable<HttpResponse<Blob>> {
    return this.http.get('/api/backup/export', { observe: 'response', responseType: 'blob' });
  }

  /** Downloads a snapshot that is already stored server-side. */
  download(name: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`/api/backup/${encodeURIComponent(name)}`, {
      observe: 'response',
      responseType: 'blob',
    });
  }

  /** Deletes a stored snapshot. */
  remove(name: string): Observable<void> {
    return this.http.delete<void>(`/api/backup/${encodeURIComponent(name)}`);
  }

  /** Applies a snapshot file; the API answers with a per-table inserted/updated report. */
  restore(file: File, mode: RestoreMode): Observable<RestoreReport> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('mode', mode);
    return this.http.post<RestoreReport>('/api/backup/restore', form);
  }
}

/** Pulls the file name out of a `Content-Disposition` header, falling back to a generic name. */
export function backupFileName(disposition: string | null, fallback = 'marketplace-backup.json'): string {
  const match = /filename="?([^";]+)"?/i.exec(disposition ?? '');
  return match?.[1] ?? fallback;
}
