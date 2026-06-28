import { Component, OnInit, computed, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';
import { InterviewDashboard } from '../interview-dashboard/interview-dashboard';
import { InterviewSetup } from '../interview-setup/interview-setup';
import { InterviewChat } from '../interview-chat/interview-chat';
import { InterviewScorecardComponent } from '../interview-scorecard/interview-scorecard';
import { InterviewService } from '../../../core/services/interview.service';
import {
  InterviewMessageDto,
  InterviewOptionsDto,
  InterviewSessionDto,
  InterviewScorecardDto,
  InterviewHistoryItemDto,
  StartSessionRequestDto,
  SubmitAnswerRequestDto
} from '../../../core/models/interview.model';

@Component({
  selector: 'app-interview',
  imports: [
    CommonModule,
    ConfirmModal,
    InterviewDashboard,
    InterviewSetup,
    InterviewChat,
    InterviewScorecardComponent
  ],
  templateUrl: './interview-shell.html',
  styleUrl: './interview-shell.css',
})
export class InterviewShell implements OnInit {
  private readonly interviewService = inject(InterviewService);
  private readonly router = inject(Router);

  view = signal<'setup' | 'dashboard' | 'chat' | 'scorecard'>('setup');
  options = signal<InterviewOptionsDto | null>(null);
  session = signal<InterviewSessionDto | null>(null);
  scorecard = signal<InterviewScorecardDto | null>(null);
  scorecardSessionId = signal<number | null>(null);
  history = signal<InterviewHistoryItemDto[]>([]);
  selectedTrack = signal('');
  selectedTrackFilter = signal<string | null>(null);
  selectedDifficulty = signal('');
  targetRole = signal('');
  inputText = signal('');
  isBotTyping = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);
  converting = signal(false);
  convertError = signal<string | null>(null);
  lastSubmittedAnswer = signal<string | null>(null);
  viewedScorecardMeta = signal<InterviewHistoryItemDto | null>(null);
  pendingDeleteId = signal<number | null>(null);
  deleting = signal(false);
  streamingBubble = signal<{ text: string; usedFallback: boolean } | null>(null);
  private streamController: AbortController | null = null;

  trackCounts = computed<Record<string, number>>(() => {
    const counts: Record<string, number> = {};
    for (const item of this.history()) {
      counts[item.track] = (counts[item.track] ?? 0) + 1;
    }
    return counts;
  });

  trackOptions = computed(() => this.options()?.tracks ?? []);
  hasActiveInProgress = computed(() => this.session()?.status === 'Active');

  ngOnInit(): void {
    this.loadOptions();
    this.loadInitialData();
  }

  private loadOptions(): void {
    this.interviewService.getOptions().subscribe({
      next: (data) => this.options.set(data)
    });
  }

  private loadInitialData(): void {
    forkJoin({
      active: this.interviewService.getActiveSession(),
      history: this.interviewService.getHistory()
    }).subscribe({
      next: ({ active, history }) => {
        this.history.set(history);
        if (active) {
          this.session.set(active);
        }

        if (history.length > 0) {
          this.view.set('dashboard');
        } else if (active) {
          this.view.set('chat');
        }
      }
    });
  }

  viewLastScorecard(): void {
    const last = this.history()[0];
    if (!last) return;
    this.loadScorecardById(last.id);
  }

  setupNewInterview(): void {
    this.view.set('setup');
  }

  resumeActiveInterview(): void {
    this.view.set('chat');
  }

  selectTrackFromDashboard(trackValue: string): void {
    this.selectedTrackFilter.set(
      this.selectedTrackFilter() === trackValue ? null : trackValue
    );
  }

  clearTrackFilter(): void {
    this.selectedTrackFilter.set(null);
  }

  setSelectedTrack(value: string): void { this.selectedTrack.set(value); }
  setSelectedDifficulty(value: string): void { this.selectedDifficulty.set(value); }
  setTargetRole(value: string): void { this.targetRole.set(value); }
  setInputText(value: string): void { this.inputText.set(value); }

  startSession(): void {
    if (!this.selectedTrack() || !this.selectedDifficulty() || !this.targetRole().trim()) return;

    this.loading.set(true);
    this.error.set(null);

    const req: StartSessionRequestDto = {
      track: this.selectedTrack(),
      difficulty: this.selectedDifficulty(),
      targetRole: this.targetRole().trim()
    };

    this.interviewService.startSession(req).subscribe({
      next: (session) => {
        this.session.set(session);
        this.view.set('chat');
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || 'Failed to start session. Please try again.');
      }
    });
  }

  sendMessage(): void {
    const text = this.inputText().trim();
    if (!text || this.isBotTyping() || (this.session()?.status === 'Completed')) return;

    const currentSession = this.session();
    if (!currentSession) return;

    const optimistic: InterviewMessageDto = {
      id: -Date.now() - 1,
      role: 'Candidate',
      turnNumber: 0,
      content: text,
      createdAt: ''
    };

    this.session.set({
      ...currentSession,
      messages: [...currentSession.messages, optimistic]
    });
    this.isBotTyping.set(true);
    this.streamingBubble.set({ text: '', usedFallback: false });
    this.lastSubmittedAnswer.set(text);
    this.error.set(null);
    this.inputText.set('');

    this.sendMessageStreamed(text, currentSession);
  }

  private sendMessageStreamed(text: string, currentSession: InterviewSessionDto): void {
    let usedFallback = false;
    let streamedContent = '';
    const nextTurn = currentSession.questionsAsked + 1;

    this.streamController = this.interviewService.submitAnswerStream(
      currentSession.id,
      { answer: text },
      {
        onToken: (content) => {
          streamedContent += content;
          this.streamingBubble.set({ text: streamedContent, usedFallback });
        },
        onDone: () => {
          this.streamingBubble.set(null);
          this.isBotTyping.set(false);
          this.lastSubmittedAnswer.set(null);
          this.streamController = null;

          const newMessage: InterviewMessageDto = {
            id: -Date.now(),
            role: 'Interviewer',
            turnNumber: nextTurn,
            content: streamedContent,
            createdAt: ''
          };
          // Use the live session (has the optimistic candidate message) instead
          // of the closure-captured one — otherwise the optimistic message is
          // dropped from the signal even though it's safely in the DB.
          const liveSession = this.session();
          if (liveSession) {
            this.session.set({
              ...liveSession,
              questionsAsked: nextTurn,
              messages: [...liveSession.messages, newMessage]
            });
          }
        },
        onError: (code, message) => {
          if (code === 'conflict') {
            this.streamingBubble.set(null);
            this.isBotTyping.set(false);
            this.streamController = null;
            this.interviewService.reloadActiveSession().subscribe({
              next: (updated) => {
                if (updated) {
                  this.session.set(updated);
                }
              }
            });
          } else if (code === 'fallback') {
            usedFallback = true;
            this.streamingBubble.set({ text: streamedContent, usedFallback: true });
          } else {
            this.error.set(message);
          }
        },
        onFatal: (message) => {
          this.streamingBubble.set(null);
          this.isBotTyping.set(false);
          this.lastSubmittedAnswer.set(text);
          this.error.set(message);
          this.streamController = null;
        }
      }
    );
  }

  retryLastAnswer(): void {
    const text = this.lastSubmittedAnswer();
    if (!text || this.isBotTyping() || (this.session()?.status === 'Completed')) return;

    const currentSession = this.session();
    if (!currentSession) return;

    this.isBotTyping.set(true);
    this.error.set(null);

    const req: SubmitAnswerRequestDto = { answer: text };

    this.interviewService.submitAnswer(currentSession.id, req).subscribe({
      next: (session) => {
        this.session.set(session);
        this.isBotTyping.set(false);
        this.lastSubmittedAnswer.set(null);
      },
      error: (err) => {
        this.isBotTyping.set(false);
        if (err.status === 409) {
          this.interviewService.reloadActiveSession().subscribe({
            next: (updated) => {
              if (updated) {
                this.session.set(updated);
              }
            }
          });
        } else {
          this.error.set('Failed to submit answer. Please try again.');
        }
      }
    });
  }

  loadScorecard(): void {
    const sessionId = this.session()?.id;
    if (!sessionId) return;
    this.loadScorecardById(sessionId);
  }

  loadScorecardById(sessionId: number): void {
    this.loading.set(true);
    const meta = this.history().find(h => h.id === sessionId) ?? null;
    this.viewedScorecardMeta.set(meta);
    this.interviewService.getScorecard(sessionId).subscribe({
      next: (sc) => {
        this.scorecard.set(sc);
        this.scorecardSessionId.set(sessionId);
        this.view.set('scorecard');
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load scorecard. Please try again.');
      }
    });
  }

  newSession(): void {
    this.session.set(null);
    this.scorecard.set(null);
    this.scorecardSessionId.set(null);
    this.viewedScorecardMeta.set(null);
    this.selectedTrack.set('');
    this.selectedTrackFilter.set(null);
    this.selectedDifficulty.set('');
    this.targetRole.set('');
    this.inputText.set('');
    this.isBotTyping.set(false);
    this.error.set(null);
    this.lastSubmittedAnswer.set(null);

    if (this.history().length > 0) {
      this.view.set('dashboard');
    } else {
      this.view.set('setup');
    }
  }

  requestDeleteSession(payload: { sessionId: number; event: MouseEvent }): void {
    payload.event.stopPropagation();
    this.pendingDeleteId.set(payload.sessionId);
  }

  cancelDelete(): void {
    if (this.deleting()) return;
    this.pendingDeleteId.set(null);
  }

  confirmDeleteSession(): void {
    const sessionId = this.pendingDeleteId();
    if (sessionId === null || this.deleting()) return;

    const snapshot = this.history();
    this.history.set(snapshot.filter(s => s.id !== sessionId));
    this.deleting.set(true);
    this.pendingDeleteId.set(null);

    this.interviewService.deleteSession(sessionId).subscribe({
      next: () => {
        this.deleting.set(false);
        this.viewedScorecardMeta.set(null);
        this.scorecard.set(null);
        this.scorecardSessionId.set(null);
      },
      error: () => {
        this.deleting.set(false);
        this.history.set(snapshot);
        this.error.set('Failed to delete session. Please try again.');
        if (this.view() === 'scorecard') {
          this.view.set('dashboard');
        }
      }
    });
  }

  printScorecard(): void {
    window.print();
  }

  convertToRoadmap(): void {
    const sessionId = this.scorecardSessionId();
    if (sessionId === null || this.converting()) return;

    this.converting.set(true);
    this.convertError.set(null);

    this.interviewService.convertScorecardToRoadmap(sessionId).subscribe({
      next: () => {
        this.converting.set(false);
        this.router.navigate(['/roadmap']);
      },
      error: (err) => {
        this.converting.set(false);
        this.convertError.set(
          err.error?.message
            ?? 'Failed to convert scorecard. Please generate a roadmap first, then try again.'
        );
      }
    });
  }
}
