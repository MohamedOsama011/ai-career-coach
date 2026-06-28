import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import {
  InterviewOptionsDto,
  StartSessionRequestDto,
  InterviewSessionDto,
  SubmitAnswerRequestDto,
  InterviewScorecardDto,
  InterviewHistoryItemDto,
  InterviewStreamCallbacks,
  InterviewStreamEvent
} from '../models/interview.model';
import { UserRoadmapDto } from '../models/roadmap.model';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class InterviewService {
  private apiUrl = 'https://localhost:7222/api/interview';
  private authService = inject(AuthService);

  constructor(private http: HttpClient) {}

  getOptions(): Observable<InterviewOptionsDto> {
    return this.http.get<InterviewOptionsDto>(`${this.apiUrl}/options`).pipe(
      timeout(7000)
    );
  }

  startSession(req: StartSessionRequestDto): Observable<InterviewSessionDto> {
    return this.http.post<InterviewSessionDto>(`${this.apiUrl}/sessions`, req).pipe(
      timeout(15000)
    );
  }

  getActiveSession(): Observable<InterviewSessionDto | null> {
    return this.http.get<InterviewSessionDto>(`${this.apiUrl}/sessions/active`).pipe(
      timeout(7000),
      catchError((err) => {
        if (err.status === 404) return of(null);
        throw err;
      })
    );
  }

  submitAnswer(sessionId: number, req: SubmitAnswerRequestDto): Observable<InterviewSessionDto> {
    return this.http.post<InterviewSessionDto>(`${this.apiUrl}/sessions/${sessionId}/answers`, req).pipe(
      timeout(30000)
    );
  }

  reloadActiveSession(): Observable<InterviewSessionDto | null> {
    return this.getActiveSession();
  }

  getScorecard(sessionId: number): Observable<InterviewScorecardDto> {
    return this.http.get<InterviewScorecardDto>(`${this.apiUrl}/sessions/${sessionId}/scorecard`).pipe(
      timeout(30000)
    );
  }

  getHistory(): Observable<InterviewHistoryItemDto[]> {
    return this.http.get<InterviewHistoryItemDto[]>(`${this.apiUrl}/sessions`).pipe(
      timeout(7000),
      catchError(() => of([]))
    );
  }

  convertScorecardToRoadmap(sessionId: number): Observable<UserRoadmapDto> {
    return this.http.post<UserRoadmapDto>(`${this.apiUrl}/sessions/${sessionId}/convert-to-roadmap`, {})
      .pipe(timeout(30000));
  }

  deleteSession(sessionId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/sessions/${sessionId}`)
      .pipe(timeout(7000));
  }

  submitAnswerStream(
    sessionId: number,
    req: SubmitAnswerRequestDto,
    callbacks: InterviewStreamCallbacks
  ): AbortController {
    const controller = new AbortController();
    const timeoutMs = 45000;
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

    const token = this.authService.getToken();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      'Accept': 'text/event-stream'
    };
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    fetch(`${this.apiUrl}/sessions/${sessionId}/answers`, {
      method: 'POST',
      headers,
      body: JSON.stringify(req),
      signal: controller.signal
    })
      .then(async response => {
        if (!response.ok) {
          if (response.status === 409) {
            callbacks.onError('conflict', 'Session was advanced by another action. Reloading.');
          } else {
            const errorText = await response.text().catch(() => '');
            callbacks.onFatal(`Server returned ${response.status}${errorText ? ': ' + errorText : ''}`);
          }
          return;
        }

        const reader = response.body?.getReader();
        if (!reader) {
          callbacks.onFatal('Stream response has no body.');
          return;
        }

        const decoder = new TextDecoder();
        let buffer = '';
        let usedFallback = false;

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;
          buffer += decoder.decode(value, { stream: true });

          const events = buffer.split('\n\n');
          buffer = events.pop() ?? '';

          for (const event of events) {
            const line = event.trim();
            if (!line.startsWith('data:')) continue;
            const json = line.slice(5).trim();
            if (!json) continue;
            try {
              const parsed = JSON.parse(json) as InterviewStreamEvent;
              if (parsed.type === 'token') {
                callbacks.onToken(parsed.content);
              } else if (parsed.type === 'error') {
                if (parsed.code === 'fallback') usedFallback = true;
                callbacks.onError(parsed.code, parsed.message);
              } else if (parsed.type === 'done') {
                callbacks.onDone(usedFallback);
                return;
              }
            } catch {
            }
          }
        }
      })
      .catch(err => {
        if (err.name === 'AbortError') {
          callbacks.onFatal('Request timed out. Please try again.');
        } else {
          callbacks.onFatal(err?.message ?? 'Network error');
        }
      })
      .finally(() => clearTimeout(timeoutId));

    return controller;
  }
}
