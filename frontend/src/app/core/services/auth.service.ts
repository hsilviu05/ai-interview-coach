import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, AuthResponse } from '../models/auth.models';
import { StorageService } from './storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(StorageService);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  login(payload: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, payload).pipe(
      tap(response => {
        this.storage.setToken(response.token);

        const role =
          response.user?.role ??
          this.readRoleFromJwt(response.token);

        if (role) {
          this.storage.setRole(role);
        }
      })
    );
  }

  register(payload: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, payload);
  }

  logout(): void {
    this.storage.clearAll();
  }

  isAuthenticated(): boolean {
    return !!this.storage.getToken();
  }

  getRole(): string | null {
    return this.storage.getRole();
  }

  getToken(): string | null {
    return this.storage.getToken();
  }

  private readRoleFromJwt(token: string): string | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return (
        payload['role'] ??
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
        null
      );
    } catch {
      return null;
    }
  }
}