export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role: 'Candidate' | 'Interviewer' | 'Admin';
}

export interface AuthResponse {
  token: string;
  expiresAt?: string;
  user?: {
    id: string;
    fullName: string;
    email: string;
    role: string;
  };
}