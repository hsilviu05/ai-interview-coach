import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, firstValueFrom, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  AuthSessionResponseDto,
  LoginRequestDto,
  RegisterRequestDto,
} from '../api/generated/backend-api';
import { LoginRequest, RegisterRequest, AuthSession } from '../models/auth.models';
import { StorageService } from './storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(StorageService);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;
  private readonly currentUser = signal<AuthSession | null>(null);
  private initializationPromise: Promise<void> | null = null;

  initialize(): Promise<void> {
    if (this.initializationPromise) {
      return this.initializationPromise;
    }

    this.clearLegacyAuthStorage();

    this.initializationPromise = firstValueFrom(
      this.http.get<AuthSessionResponseDto>(`${this.baseUrl}/me`).pipe(
        tap(session => {
          this.currentUser.set(this.normalizeSession(session));
        }),
        catchError(() => {
          this.currentUser.set(null);
          return of(null);
        })
      )
    ).then(() => undefined);

    return this.initializationPromise;
  }

  login(payload: LoginRequest): Observable<AuthSession> {
    return this.http.post<AuthSessionResponseDto>(`${this.baseUrl}/login`, payload as LoginRequestDto).pipe(
      tap(session => {
        this.currentUser.set(this.normalizeSession(session));
      }),
      map(session => this.normalizeSession(session))
    );
  }

  register(payload: RegisterRequest): Observable<AuthSession> {
    return this.http.post<AuthSessionResponseDto>(`${this.baseUrl}/register`, payload as RegisterRequestDto).pipe(
      tap(session => {
        this.currentUser.set(this.normalizeSession(session));
      }),
      map(session => this.normalizeSession(session))
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {}).pipe(
      tap(() => {
        this.currentUser.set(null);
      }),
      catchError(() => {
        this.currentUser.set(null);
        return of(void 0);
      })
    );
  }

  isAuthenticated(): boolean {
    return this.currentUser() !== null;
  }

  getRole(): string | null {
    return this.currentUser()?.role ?? null;
  }

  getCurrentUser(): AuthSession | null {
    return this.currentUser();
  }

  private clearLegacyAuthStorage(): void {
    this.storage.removeItem('auth_token');
    this.storage.removeItem('auth_role');
  }

  private normalizeSession(session: AuthSessionResponseDto): AuthSession {
    return {
      fullName: session.fullName ?? '',
      email: session.email ?? '',
      role: session.role ?? '',
    };
  }
}
