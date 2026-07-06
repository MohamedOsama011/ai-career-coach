import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GeneralResponse, SubscriberDetailDto, AuditLogDto, ExtendSubscriptionRequest } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class AdminSubscriptionService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7222/api/admin/subscriptions';
  private paymentUrl = 'https://localhost:7222/api/admin/payments';

  getDetail(id: number): Observable<GeneralResponse<SubscriberDetailDto>> {
    return this.http.get<GeneralResponse<SubscriberDetailDto>>(`${this.apiUrl}/${id}/detail`);
  }

  activate(id: number, notes?: string): Observable<GeneralResponse<string>> {
    return this.http.post<GeneralResponse<string>>(`${this.apiUrl}/${id}/activate`, { notes });
  }

  cancel(id: number, notes?: string, immediate = false): Observable<GeneralResponse<string>> {
    return this.http.post<GeneralResponse<string>>(`${this.apiUrl}/${id}/cancel`, { notes, immediate });
  }

  extend(id: number, request: ExtendSubscriptionRequest): Observable<GeneralResponse<string>> {
    return this.http.post<GeneralResponse<string>>(`${this.apiUrl}/${id}/extend`, request);
  }

  markPaymentPaid(paymentId: number, notes?: string): Observable<GeneralResponse<string>> {
    return this.http.post<GeneralResponse<string>>(`${this.paymentUrl}/${paymentId}/mark-paid`, { notes });
  }

  refundPayment(paymentId: number, notes?: string): Observable<GeneralResponse<string>> {
    return this.http.post<GeneralResponse<string>>(`${this.paymentUrl}/${paymentId}/refund`, { notes });
  }

  getAuditLog(subscriptionId: number): Observable<GeneralResponse<AuditLogDto[]>> {
    return this.http.get<GeneralResponse<AuditLogDto[]>>(`${this.apiUrl}/${subscriptionId}/audit-log`);
  }
}
