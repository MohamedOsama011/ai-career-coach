import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GeneralResponse, CreatePaymentRequest } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7222/api/fawaterak';

  createPayment(dto: CreatePaymentRequest): Observable<GeneralResponse> {
    return this.http.post<GeneralResponse>(`${this.apiUrl}/create-payment`, dto);
  }

  executeInvoice(methodId: string, userSubscriptionId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/execute-invoice`, null, {
      params: { methodId, userSubscriptionId },
    });
  }
}
