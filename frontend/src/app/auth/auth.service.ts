import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Credentials {
  username: string;
  password: string;
}

export interface Session {
  userId: string;
  username: string;
  token: string;
  expiresAt: string;
}

const storageKey = 'session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private httpClient = inject(HttpClient);
  private router = inject(Router);

  private session = signal<Session | null>(loadSession());

  readonly username = computed(() => this.session()?.username ?? null);

  token(): string | null {
    return this.session()?.token ?? null;
  }

  isLoggedIn(): boolean {
    const session = this.session();
    return session !== null && !sessionExpired(session);
  }

  login(credentials: Credentials): Observable<Session> {
    return this.httpClient
      .post<Session>(`${environment.apiUrl}/auth/login`, credentials)
      .pipe(tap((session) => this.save(session)));
  }

  register(credentials: Credentials): Observable<Session> {
    return this.httpClient
      .post(`${environment.apiUrl}/auth/register`, credentials)
      .pipe(switchMap(() => this.login(credentials)));
  }

  logout(): void {
    localStorage.removeItem(storageKey);
    this.session.set(null);
    this.router.navigate(['/login']);
  }

  private save(session: Session): void {
    localStorage.setItem(storageKey, JSON.stringify(session));
    this.session.set(session);
  }
}

function loadSession(): Session | null {
  try {
    const session = JSON.parse(localStorage.getItem(storageKey) ?? 'null') as Session | null;
    return session && !sessionExpired(session) ? session : null;
  } catch {
    return null;
  }
}

function sessionExpired(session: Session): boolean {
  return new Date(session.expiresAt).getTime() <= Date.now();
}
