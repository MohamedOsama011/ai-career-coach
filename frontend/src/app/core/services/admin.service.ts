import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardStatistics, AdminUser, CVAdmin, UserManagement, ChangeRoleRequest, PaginatedSessionsResult, SyncLogDto, UserDetailDto, PaginatedAuditLogs, HealthCheckDto, ReportsDto, PaginatedChatSessionsDto, ChatMessageAdminDto } from '../models/admin.model';
import { BroadcastRequest } from '../models/notification.model';

import { API_BASE_URL } from '../api-config';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = `${API_BASE_URL}/api/admin`;

  getStatistics(): Observable<DashboardStatistics> {
    return this.http.get<DashboardStatistics>(`${this.apiUrl}/statistics`);
  }

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.apiUrl}/users`);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/users/${id}`);
  }

  changeRole(id: string, role: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/users/${id}/role`, { role } as ChangeRoleRequest);
  }

  getCVs(): Observable<CVAdmin[]> {
    return this.http.get<CVAdmin[]>(`${this.apiUrl}/cvs`);
  }

  deleteCV(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/cvs/${id}`);
  }

  downloadCV(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/cvs/${id}/download`, { responseType: 'blob' });
  }

  getUserManagement(): Observable<UserManagement[]> {
    return this.http.get<UserManagement[]>(`${this.apiUrl}/user-management`);
  }

  getInterviewSessions(params: {
    page?: number;
    pageSize?: number;
    status?: string;
    track?: string;
    difficulty?: string;
    from?: string;
    to?: string;
  }): Observable<PaginatedSessionsResult> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.track) httpParams = httpParams.set('track', params.track);
    if (params.difficulty) httpParams = httpParams.set('difficulty', params.difficulty);
    if (params.from) httpParams = httpParams.set('from', params.from);
    if (params.to) httpParams = httpParams.set('to', params.to);
    return this.http.get<PaginatedSessionsResult>(`${this.apiUrl}/interviews/sessions`, { params: httpParams });
  }

  deleteInterviewSession(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/interviews/sessions/${id}`);
  }

  abortInterviewSession(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/interviews/sessions/${id}/abort`, {});
  }

  getSyncLogs(count: number = 50): Observable<SyncLogDto[]> {
    return this.http.get<SyncLogDto[]>(`${this.apiUrl}/sync-logs`, {
      params: new HttpParams().set('count', count),
    });
  }

  getUserDetail(id: string): Observable<UserDetailDto> {
    return this.http.get<UserDetailDto>(`${this.apiUrl}/users/${id}/detail`);
  }

  clearCache(userId?: string): Observable<void> {
    let params = new HttpParams();
    if (userId) params = params.set('userId', userId);
    return this.http.delete<void>(`${this.apiUrl}/cache`, { params });
  }

  getHealth(): Observable<HealthCheckDto> {
    return this.http.get<HealthCheckDto>(`${this.apiUrl}/health`);
  }

  getReports(): Observable<ReportsDto> {
    return this.http.get<ReportsDto>(`${this.apiUrl}/reports`);
  }

  exportCsv(type: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/reports/export`, {
      params: new HttpParams().set('type', type),
      responseType: 'blob',
    });
  }

  getAuditLogs(params: {
    page?: number;
    pageSize?: number;
    action?: string;
    adminId?: string;
  }): Observable<PaginatedAuditLogs> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.action) httpParams = httpParams.set('action', params.action);
    if (params.adminId) httpParams = httpParams.set('adminId', params.adminId);
    return this.http.get<PaginatedAuditLogs>(`${this.apiUrl}/audit-logs`, { params: httpParams });
  }

  broadcast(request: BroadcastRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/broadcast`, request);
  }

  getChatSessions(params: { page?: number; pageSize?: number }): Observable<PaginatedChatSessionsDto> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    return this.http.get<PaginatedChatSessionsDto>(`${this.apiUrl}/chat-sessions`, { params: httpParams });
  }

  getChatMessages(sessionId: number): Observable<ChatMessageAdminDto[]> {
    return this.http.get<ChatMessageAdminDto[]>(`${this.apiUrl}/chat-sessions/${sessionId}/messages`);
  }
}
