import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedNotificationsDto, UnreadCountDto } from '../models/notification.model';

import { API_BASE_URL } from '../api-config';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private apiUrl = `${API_BASE_URL}/api/notification`;

  getNotifications(page = 1, pageSize = 20): Observable<PaginatedNotificationsDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PaginatedNotificationsDto>(this.apiUrl, { params });
  }

  getUnreadCount(): Observable<UnreadCountDto> {
    return this.http.get<UnreadCountDto>(`${this.apiUrl}/unread-count`);
  }

  markAsRead(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/read`, {});
  }

  markAllAsRead(): Observable<{ count: number }> {
    return this.http.post<{ count: number }>(`${this.apiUrl}/read-all`, {});
  }
}
