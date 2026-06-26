import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import {
  InterviewOptionsDto,
  StartSessionRequestDto,
  InterviewSessionDto,
  SubmitAnswerRequestDto,
  InterviewScorecardDto,
  InterviewHistoryItemDto
} from '../models/interview.model';

@Injectable({
  providedIn: 'root'
})
export class InterviewService {
  private apiUrl = 'https://localhost:7222/api/interview';

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
}
