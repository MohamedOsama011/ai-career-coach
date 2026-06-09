import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { AuthResponse, LoginRequest, RegisterRequest } from "../models/user.model";
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
    saveToken(token: string): void {
        localStorage.setItem('authToken', token);
    }
    saveUserInfo(fullName: string, email: string): void {
        localStorage.setItem('userFullName', fullName);
        localStorage.setItem('userEmail', email);
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
    logout(): void {
        localStorage.removeItem('authToken');
        localStorage.removeItem('userFullName');
        localStorage.removeItem('userEmail');
    }
}

