import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GeneralResponse, UserSubscriptionDto, PaymentInvoiceDto, PagedPaymentHistoryDto, SubscriberDetailDto } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class UserSubscriptionService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7222/api/usersubscription';

  getMy(): Observable<GeneralResponse<UserSubscriptionDto[]>> {
    return this.http.get<GeneralResponse<UserSubscriptionDto[]>>(`${this.apiUrl}/my`);
  }

  getAll(search?: string, from?: Date, to?: Date): Observable<GeneralResponse<UserSubscriptionDto[]>> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    if (from) params = params.set('from', from.toISOString());
    if (to) params = params.set('to', to.toISOString());
    return this.http.get<GeneralResponse<UserSubscriptionDto[]>>(`${this.apiUrl}/all`, { params });
  }

  getDetail(id: number): Observable<GeneralResponse<SubscriberDetailDto>> {
    return this.http.get<GeneralResponse<SubscriberDetailDto>>(`${this.apiUrl}/${id}/detail`);
  }

  cancelSubscription(id: number): Observable<GeneralResponse<string>> {
    return this.http.post<GeneralResponse<string>>(`${this.apiUrl}/${id}/cancel`, {});
  }

  getPaymentHistory(page = 1, pageSize = 20): Observable<GeneralResponse<PagedPaymentHistoryDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<GeneralResponse<PagedPaymentHistoryDto>>(`${this.apiUrl}/my/payments`, { params });
  }

  getPaymentInvoice(paymentId: number): Observable<GeneralResponse<PaymentInvoiceDto>> {
    return this.http.get<GeneralResponse<PaymentInvoiceDto>>(`${this.apiUrl}/payments/${paymentId}/invoice`);
  }
}
