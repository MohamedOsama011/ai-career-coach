import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RevenueAnalyticsDto } from '../models/payment.model';


import { API_BASE_URL } from '../api-config';

@Injectable({ providedIn: 'root' })
export class RevenueService {
  private http = inject(HttpClient);
  private apiUrl = `${API_BASE_URL}/api/usersubscription`;

  getAnalytics(from?: Date, to?: Date): Observable<RevenueAnalyticsDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from.toISOString());
    if (to) params = params.set('to', to.toISOString());
    return this.http.get<RevenueAnalyticsDto>(`${this.apiUrl}/analytics`, { params });
  }
}
