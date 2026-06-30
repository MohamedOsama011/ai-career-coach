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

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  password: string;
  confirmPassword: string;
}

export interface GeneralResponse {
  success: boolean;
  data: any;
}

export interface ProfileResponse {
  fullName: string;
  email: string;
  careerGoal: string;
  createdAt: string;
  cvCount: number;
  roles: string[];
}
