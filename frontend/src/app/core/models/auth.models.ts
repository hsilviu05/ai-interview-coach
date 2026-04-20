import type {
  LoginRequestDto,
  RegisterRequestDto,
} from '../api/generated/backend-api';

export type LoginRequest = LoginRequestDto;

export type RegisterRequest = RegisterRequestDto;

export interface AuthSession {
  fullName: string;
  email: string;
  role: string;
}
