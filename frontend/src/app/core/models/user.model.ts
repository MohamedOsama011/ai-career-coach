export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  fullName: string;
  email: string;
  roles: string[];
}

export interface User {
  id: string;
  fullName: string;
  email: string;
  careerGoal?: string;
  createdAt: Date;
}
