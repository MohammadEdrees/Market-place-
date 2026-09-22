import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthUser, LoginResponse } from './models';

const STORAGE_KEY = 'marketplace.auth';

interface StoredSession {
  token: string;
  expiresAt: string;
  user: AuthUser;
}

/**
 * Holds the JWT session (localStorage-persisted) and exposes auth state as signals.
 * Talks to `POST /api/auth/login`; the attached interceptor sends the token on every
 * API call and calls `logout()` when the API answers 401.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<StoredSession | null>(this.restore());

  /** Profile of the signed-in user, or `null` when signed out. */
  readonly user = computed(() => this.session()?.user ?? null);

  /** Raw JWT to send as `Authorization: Bearer <token>`. */
  readonly token = computed(() => this.session()?.token ?? null);

  /** True while a non-expired session exists — used by the route guard. */
  readonly isAuthenticated = computed(() => this.session() !== null);

  /** Exchanges credentials for a token and persists the session. */
  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      tap((response) => {
        const session: StoredSession = {
          token: response.token,
          expiresAt: response.expiresAt,
          user: response.user,
        };
        // Persist so a page reload keeps the session (see restore()).
        localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
        this.session.set(session);
      }),
    );
  }

  /** Clears the session (also invoked by the interceptor on 401) and returns to the login page. */
  logout(): void {
    this.session.set(null);
    localStorage.removeItem(STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  private restore(): StoredSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return null;
      }

      const session = JSON.parse(raw) as StoredSession;
      if (!session?.token || Date.parse(session.expiresAt) <= Date.now()) {
        // Expired or malformed — drop it so the guard sends the user to /login.
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }

      return session;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }
}
