import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, delay, timeout, map } from 'rxjs/operators';
import {
  ChatMessage,
  InterviewTrack,
  InterviewOptionsDto,
  StartSessionRequestDto,
  InterviewSessionDto,
  SubmitAnswerRequestDto,
  InterviewScorecardDto,
  InterviewHistoryItemDto
} from '../models/interview.model';

export type { ChatMessage, InterviewTrack };

@Injectable({
  providedIn: 'root'
})
export class InterviewService {
  private apiUrl = 'https://localhost:7222/api/interview';

  private mockTracks: InterviewTrack[] = [
    {
      id: 'behavioral',
      title: 'Behavioral',
      subtitle: 'STAR-based, role-aligned',
      sessionsCount: 12
    },
    {
      id: 'technical',
      title: 'Technical Coding',
      subtitle: 'Live coding with hints',
      sessionsCount: 8
    },
    {
      id: 'system',
      title: 'System Design',
      subtitle: 'Whiteboard mode',
      sessionsCount: 5
    }
  ];

  private mockInitialMessages: Record<string, ChatMessage[]> = {
    behavioral: [
      {
        id: 1,
        sender: 'bot',
        text: 'Tell me about a time you led a technical decision that the team disagreed with.',
        timestamp: new Date()
      },
      {
        id: 2,
        sender: 'user',
        text: 'At TechFlow we needed to migrate from REST to GraphQL. The team was split. I ran a 2-week spike to compare developer velocity, then presented the data.',
        timestamp: new Date()
      },
      {
        id: 3,
        sender: 'bot',
        text: "Good structure. Can you quantify the outcome and what you'd do differently?",
        timestamp: new Date()
      }
    ],
    technical: [
      {
        id: 1,
        sender: 'bot',
        text: 'Write a function that finds the longest palindromic substring in a given string. What is the time complexity?',
        timestamp: new Date()
      }
    ],
    system: [
      {
        id: 1,
        sender: 'bot',
        text: 'Design a highly available notification service that supports push notifications, SMS, and email. What are the key bottlenecks?',
        timestamp: new Date()
      }
    ]
  };

  private botResponses: string[] = [
    "That makes sense. Can you dive deeper into the technical challenges you faced during this process?",
    "Interesting approach! How did you ensure reliability and testability in that setup?",
    "Great. What metrics did you use to evaluate the success of this decision?",
    "Thanks for sharing. If you had to build this again from scratch, is there anything you would change?",
    "Excellent analysis. How did you communicate this decision to stakeholders outside the engineering team?"
  ];

  constructor(private http: HttpClient) {}

  getTracks(): Observable<InterviewTrack[]> {
    return of(this.mockTracks);
  }

  getInitialMessages(trackId: string): ChatMessage[] {
    return this.mockInitialMessages[trackId] || this.mockInitialMessages['behavioral'];
  }

  simulateBotReply(userMessage: string): Observable<string> {
    const randomIndex = Math.floor(Math.random() * this.botResponses.length);
    const reply = this.botResponses[randomIndex];
    return of(reply).pipe(delay(1500));
  }

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
