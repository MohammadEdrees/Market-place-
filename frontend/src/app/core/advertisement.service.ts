import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Advertisement, AdvertisementInput } from './models';

@Injectable({ providedIn: 'root' })
export class AdvertisementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/advertisements';

  /** Every ad, ordered by `sortOrder` then id. */
  list(): Observable<Advertisement[]> {
    return this.http.get<Advertisement[]>(this.baseUrl);
  }

  get(id: number): Observable<Advertisement> {
    return this.http.get<Advertisement>(`${this.baseUrl}/${id}`);
  }

  /** Admin creates an ad — 400 when the schedule window is invalid. */
  create(input: AdvertisementInput): Observable<Advertisement> {
    return this.http.post<Advertisement>(this.baseUrl, input);
  }

  /** Admin updates an ad — 400 on an invalid body, 404 when the id is unknown. */
  update(id: number, input: AdvertisementInput): Observable<Advertisement> {
    return this.http.put<Advertisement>(`${this.baseUrl}/${id}`, input);
  }

  /** Admin deletes an ad — the stored image file is removed server-side too. */
  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Multipart upload of the ad's single image (png/jpg/jpeg/webp/gif, ≤ 5 MB). */
  uploadImage(id: number, file: File): Observable<Advertisement> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<Advertisement>(`${this.baseUrl}/${id}/image`, form);
  }
}
