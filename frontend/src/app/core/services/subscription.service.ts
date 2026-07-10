import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GeneralResponse, SubscriptionPlan } from '../models/payment.model';


import { API_BASE_URL } from '../api-config';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private http = inject(HttpClient);
  private apiUrl = `${API_BASE_URL}/api/subscription`;

  getAll(): Observable<GeneralResponse<SubscriptionPlan[]>> {
    return this.http.get<GeneralResponse<SubscriptionPlan[]>>(this.apiUrl);
  }

  getById(id: string): Observable<GeneralResponse<SubscriptionPlan>> {
    return this.http.get<GeneralResponse<SubscriptionPlan>>(`${this.apiUrl}/${id}`);
  }

  create(body: Omit<SubscriptionPlan, 'id'>): Observable<void> {
    return this.http.post<void>(this.apiUrl, body);
  }

  update(id: string, body: Omit<SubscriptionPlan, 'id'>): Observable<GeneralResponse<string>> {
    return this.http.put<GeneralResponse<string>>(`${this.apiUrl}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
