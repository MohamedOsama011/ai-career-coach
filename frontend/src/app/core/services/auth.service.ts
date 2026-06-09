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
    getToken(): string | null {
        return localStorage.getItem('authToken');
    }
    logout(): void {
        localStorage.removeItem('authToken');
    }
}

