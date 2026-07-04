import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GeneralResponse, CreateSubscriptionRequest } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7222/api/subscription';

  getAll(): Observable<GeneralResponse> {
    return this.http.get<GeneralResponse>(this.apiUrl);
  }

  getById(id: string): Observable<GeneralResponse> {
    return this.http.get<GeneralResponse>(`${this.apiUrl}/${id}`);
  }

  create(body: CreateSubscriptionRequest): Observable<void> {
    return this.http.post<void>(this.apiUrl, body);
  }

  update(id: string, body: CreateSubscriptionRequest): Observable<GeneralResponse> {
    return this.http.put<GeneralResponse>(`${this.apiUrl}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
