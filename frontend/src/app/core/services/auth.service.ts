import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { AuthResponse, LoginRequest, RegisterRequest, ForgotPasswordRequest, ResetPasswordRequest, GeneralResponse, ProfileResponse } from "../models/user.model";
import { Observable } from "rxjs";

@Injectable({ providedIn: 'root'})

export class AuthService{
    private apiUrl = 'https://localhost:7222/api/auth';

    constructor(private http: HttpClient) {}

    register(data: RegisterRequest) : Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data);
    }

    login(data: LoginRequest) : Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.apiUrl}/login`, data);
    }

    forgotPassword(data: ForgotPasswordRequest): Observable<void> {
        return this.http.post<void>(`${this.apiUrl}/ForgotPassword`, data);
    }

    resetPassword(data: ResetPasswordRequest): Observable<GeneralResponse> {
        return this.http.post<GeneralResponse>(`${this.apiUrl}/ResetPassword`, data);
    }

    getProfile(): Observable<ProfileResponse> {
        return this.http.get<ProfileResponse>(`${this.apiUrl}/profile`);
    }

    saveToken(token: string): void {
        localStorage.setItem('authToken', token);
    }
    saveUserInfo(fullName: string, email: string, roles?: string[]): void {
        localStorage.setItem('userFullName', fullName);
        localStorage.setItem('userEmail', email);
        if (roles) {
            localStorage.setItem('userRoles', JSON.stringify(roles));
        }
    }
    getToken(): string | null {
        return localStorage.getItem('authToken');
    }
    getUserFullName(): string {
        return localStorage.getItem('userFullName') || 'User';
    }
    getUserEmail(): string {
        return localStorage.getItem('userEmail') || '';
    }
    getUserInitials(): string {
        const name = this.getUserFullName();
        const parts = name.split(' ');
        if (parts.length >= 2) {
            return (parts[0][0] + parts[1][0]).toUpperCase();
        }
        return name[0]?.toUpperCase() || 'U';
    }
    isLoggedIn(): boolean {
        return !!this.getToken();
    }
  getRoles(): string[] {
    const stored = localStorage.getItem('userRoles');
    return stored ? JSON.parse(stored) : [];
  }

  getRolesFromToken(): string[] {
    const token = this.getToken();
    if (!token) return [];
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const roleClaim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (!roleClaim) return [];
      return Array.isArray(roleClaim) ? roleClaim : [roleClaim];
    } catch {
      return [];
    }
  }

  syncRolesFromToken(): void {
    const roles = this.getRolesFromToken();
    if (roles.length > 0) {
      localStorage.setItem('userRoles', JSON.stringify(roles));
    }
  }

  isAdmin(): boolean {
    return this.getRoles().includes('Admin');
  }

    logout(): void {
        localStorage.removeItem('authToken');
        localStorage.removeItem('userFullName');
        localStorage.removeItem('userEmail');
        localStorage.removeItem('userRoles');
    }
}

