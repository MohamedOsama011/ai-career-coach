import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import {
  ChatSession,
  ChatSessionSummary,
  SendChatMessageRequest
} from '../models/chat.model';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = 'https://localhost:7222/api/chat';
  private http = inject(HttpClient);

  createSession(): Observable<ChatSession> {
    return this.http.post<ChatSession>(`${this.apiUrl}/sessions`, {})
      .pipe(timeout(7000));
  }

  getUserSessions(): Observable<ChatSessionSummary[]> {
    return this.http.get<ChatSessionSummary[]>(`${this.apiUrl}/sessions`)
      .pipe(timeout(7000), catchError(() => of([])));
  }

  getSession(sessionId: number): Observable<ChatSession> {
    return this.http.get<ChatSession>(`${this.apiUrl}/sessions/${sessionId}`)
      .pipe(timeout(7000));
  }

  sendMessage(sessionId: number, message: string): Observable<ChatSession> {
    const body: SendChatMessageRequest = { message };
    return this.http.post<ChatSession>(`${this.apiUrl}/sessions/${sessionId}/messages`, body)
      .pipe(timeout(60000));
  }
}
