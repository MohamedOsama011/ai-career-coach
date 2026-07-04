import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GeneralResponse } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class UserSubscriptionService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7222/api/usersubscription';

  getByUser(userId: string): Observable<GeneralResponse> {
    return this.http.get<GeneralResponse>(`${this.apiUrl}/by-user/${userId}`);
  }
}
