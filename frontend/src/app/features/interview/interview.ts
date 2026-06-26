import { Component, OnInit, signal, computed, ViewChild, ElementRef, effect, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Card } from '../../shared/components/card/card';
import { Badge, BadgeVariant } from '../../shared/components/badge/badge';
import { StatCard } from '../../shared/components/stat-card/stat-card';
import { InterviewService } from '../../core/services/interview.service';
import {
  InterviewMessageDto,
  InterviewOptionsDto,
  InterviewSessionDto,
  InterviewScorecardDto,
  InterviewHistoryItemDto,
  StartSessionRequestDto,
  SubmitAnswerRequestDto
} from '../../core/models/interview.model';

interface DashboardTrack {
  id: string;
  title: string;
  subtitle: string;
}

@Component({
  selector: 'app-interview',
  imports: [CommonModule, FormsModule, Card, Badge, StatCard],
  templateUrl: './interview.html',
  styleUrl: './interview.css',
})
export class Interview implements OnInit {
  view = signal<'setup' | 'dashboard' | 'chat' | 'scorecard'>('setup');
  options = signal<InterviewOptionsDto | null>(null);
  session = signal<InterviewSessionDto | null>(null);
  scorecard = signal<InterviewScorecardDto | null>(null);
  history = signal<InterviewHistoryItemDto[]>([]);
  selectedTrack = signal('');
  selectedTrackFilter = signal<string | null>(null);
  selectedDifficulty = signal('');
  targetRole = signal('');
  inputText = signal('');
  isBotTyping = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);

  @ViewChild('scrollAnchor') scrollAnchor!: ElementRef<HTMLElement>;

  messages = computed(() => this.session()?.messages ?? []);
  isCompleted = computed(() => this.session()?.status === 'Completed');
  questionsAsked = computed(() => this.session()?.questionsAsked ?? 0);
  maxQuestions = computed(() => this.session()?.maxQuestions ?? 6);
  stepArray = computed(() => Array.from({length: this.maxQuestions()}, (_, i) => i + 1));
  lastSession = computed(() => this.history().length > 0 ? this.history()[0] : null);
  hasActiveInProgress = computed(() => this.session()?.status === 'Active');
  filteredHistory = computed(() => {
    const filter = this.selectedTrackFilter();
    if (!filter) return this.history();
    return this.history().filter(item => item.track === filter);
  });

  tracks: DashboardTrack[] = [
    { id: 'Behavioral', title: 'Behavioral', subtitle: 'STAR-based, role-aligned' },
    { id: 'TechnicalCoding', title: 'Technical Coding', subtitle: 'Live coding with hints' },
    { id: 'SystemDesign', title: 'System Design', subtitle: 'Whiteboard mode' }
  ];

  constructor(private interviewService: InterviewService, private cdr: ChangeDetectorRef) {
    effect(() => {
      this.messages();
      setTimeout(() => {
        this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth' });
      });
    });
  }

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
          this.cdr.detectChanges();
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
    const last = this.lastSession();
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

  trackCount(trackValue: string): number {
    return this.history().filter(item => item.track === trackValue).length;
  }

  clearTrackFilter(): void {
    this.selectedTrackFilter.set(null);
  }

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
        this.cdr.detectChanges();
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
    if (!text || this.isBotTyping() || this.isCompleted()) return;

    const currentSession = this.session();
    if (!currentSession) return;

    const optimistic: InterviewMessageDto = {
      id: 0,
      role: 'Candidate',
      turnNumber: 0,
      content: text,
      createdAt: ''
    };

    this.session.set({
      ...currentSession,
      messages: [...currentSession.messages, optimistic]
    });
    this.cdr.detectChanges();
    this.isBotTyping.set(true);
    this.inputText.set('');

    const req: SubmitAnswerRequestDto = { answer: text };

    this.interviewService.submitAnswer(currentSession.id, req).subscribe({
      next: (session) => {
        this.session.set(session);
        this.cdr.detectChanges();
        this.isBotTyping.set(false);
      },
      error: () => {
        this.isBotTyping.set(false);
        this.error.set('Failed to submit answer. Please try again.');
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
    this.interviewService.getScorecard(sessionId).subscribe({
      next: (sc) => {
        this.scorecard.set(sc);
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
    this.cdr.detectChanges();
    this.scorecard.set(null);
    this.selectedTrack.set('');
    this.selectedTrackFilter.set(null);
    this.selectedDifficulty.set('');
    this.targetRole.set('');
    this.inputText.set('');
    this.isBotTyping.set(false);
    this.error.set(null);

    if (this.history().length > 0) {
      this.view.set('dashboard');
    } else {
      this.view.set('setup');
    }
  }

  messageSender(role: string): 'bot' | 'user' {
    return role === 'Interviewer' ? 'bot' : 'user';
  }

  stepClass(step: number): string {
    const current = this.questionsAsked();
    if (step < current) return 'completed';
    if (step === current) return 'current';
    return 'future';
  }

  ratingVariant(rating: string): BadgeVariant {
    if (rating === 'Strong') return 'success';
    if (rating === 'Adequate') return 'warning';
    if (rating === 'Weak') return 'danger';
    return 'default';
  }

  gradeClass(grade: string): string {
    if (grade === 'A' || grade === 'A-') return 'grade-a';
    if (grade === 'B+' || grade === 'B') return 'grade-b';
    if (grade === 'C') return 'grade-c';
    return 'grade-default';
  }
}
