import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class StorageService {
  private readonly tokenKey = 'auth_token';
  private readonly roleKey = 'auth_role';

  setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  clearToken(): void {
    localStorage.removeItem(this.tokenKey);
  }

  setRole(role: string): void {
    localStorage.setItem(this.roleKey, role);
  }

  getRole(): string | null {
    return localStorage.getItem(this.roleKey);
  }

  clearAll(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
  }
}