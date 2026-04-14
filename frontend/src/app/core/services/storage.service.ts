import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class StorageService {
  private readonly tokenKey = 'auth_token';
  private readonly roleKey = 'auth_role';

  setItem(key: string, value: string): void {
    localStorage.setItem(key, value);
  }

  getItem(key: string): string | null {
    return localStorage.getItem(key);
  }

  removeItem(key: string): void {
    localStorage.removeItem(key);
  }

  setToken(token: string): void {
    this.setItem(this.tokenKey, token);
  }

  getToken(): string | null {
    return this.getItem(this.tokenKey);
  }

  clearToken(): void {
    this.removeItem(this.tokenKey);
  }

  setRole(role: string): void {
    this.setItem(this.roleKey, role);
  }

  getRole(): string | null {
    return this.getItem(this.roleKey);
  }

  clearAll(): void {
    this.removeItem(this.tokenKey);
    this.removeItem(this.roleKey);
  }
}
