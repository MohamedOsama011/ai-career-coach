import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Card } from '../../shared/components/card/card';
import { Badge, BadgeVariant } from '../../shared/components/badge/badge';
import { StatCard } from '../../shared/components/stat-card/stat-card';
import { InterviewService } from '../../core/services/interview.service';
import {
  InterviewOptionsDto,
  InterviewSessionDto,
  InterviewScorecardDto,
  StartSessionRequestDto,
  SubmitAnswerRequestDto
} from '../../core/models/interview.model';

@Component({
  selector: 'app-interview',
  imports: [CommonModule, FormsModule, Card, Badge, StatCard],
  templateUrl: './interview.html',
  styleUrl: './interview.css',
})
export class Interview implements OnInit {
  view = signal<'setup' | 'chat' | 'scorecard'>('setup');
  options = signal<InterviewOptionsDto | null>(null);
  session = signal<InterviewSessionDto | null>(null);
  scorecard = signal<InterviewScorecardDto | null>(null);
  selectedTrack = signal('');
  selectedDifficulty = signal('');
  targetRole = signal('');
  inputText = signal('');
  isBotTyping = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);

  messages = computed(() => this.session()?.messages ?? []);
  isCompleted = computed(() => this.session()?.status === 'Completed');

  constructor(private interviewService: InterviewService) {}

  ngOnInit(): void {
    this.loadOptions();
    this.resumeActiveSession();
  }

  private loadOptions(): void {
    this.interviewService.getOptions().subscribe({
      next: (data) => this.options.set(data)
    });
  }

  private resumeActiveSession(): void {
    this.interviewService.getActiveSession().subscribe({
      next: (session) => {
        if (session) {
          this.session.set(session);
          this.view.set('chat');
        }
      }
    });
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

    const sessionId = this.session()?.id;
    if (!sessionId) return;

    this.isBotTyping.set(true);
    this.inputText.set('');

    const req: SubmitAnswerRequestDto = { answer: text };

    this.interviewService.submitAnswer(sessionId, req).subscribe({
      next: (session) => {
        this.session.set(session);
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
    this.view.set('setup');
    this.session.set(null);
    this.scorecard.set(null);
    this.selectedTrack.set('');
    this.selectedDifficulty.set('');
    this.targetRole.set('');
    this.inputText.set('');
    this.isBotTyping.set(false);
    this.error.set(null);
  }

  messageSender(role: string): 'bot' | 'user' {
    return role === 'Interviewer' ? 'bot' : 'user';
  }

  ratingVariant(rating: string): BadgeVariant {
    if (rating === 'Strong') return 'success';
    if (rating === 'Adequate') return 'warning';
    if (rating === 'Weak') return 'danger';
    return 'default';
  }
}
