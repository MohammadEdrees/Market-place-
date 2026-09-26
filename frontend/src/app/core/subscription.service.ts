import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Subscription,
  SubscriptionCreateInput,
  SubscriptionUpdateInput,
} from './models';

/** CRUD for the plans bound to accounts (`/api/subscriptions`). */
@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/subscriptions';

  /** Every plan with its subscriber (`userName`/`userEmail`/`userType`), newest first. */
  list(): Observable<Subscription[]> {
    return this.http.get<Subscription[]>(this.baseUrl);
  }

  get(id: number): Observable<Subscription> {
    return this.http.get<Subscription>(`${this.baseUrl}/${id}`);
  }

  /**
   * The plans held by one account — this is the mobile-user link: pass the id of a
   * `Mobile` profile to show what that app user is subscribed to.
   */
  listForUser(userId: number): Observable<Subscription[]> {
    return this.http.get<Subscription[]>(`${this.baseUrl}/user/${userId}`);
  }

  /** Admin attaches a plan to an account → 400 when the account does not exist. */
  create(input: SubscriptionCreateInput): Observable<Subscription> {
    return this.http.post<Subscription>(this.baseUrl, input);
  }

  /** Admin updates plan/price/cycle/status/renewal. */
  update(id: number, input: SubscriptionUpdateInput): Observable<Subscription> {
    return this.http.put<Subscription>(`${this.baseUrl}/${id}`, input);
  }

  /** Admin removes a plan → 204. */
  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
