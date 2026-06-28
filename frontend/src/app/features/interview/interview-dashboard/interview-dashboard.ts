import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Card } from '../../../shared/components/card/card';
import { InterviewHistoryItemDto, InterviewOptionItem } from '../../../core/models/interview.model';

const TRACK_LABELS: Record<string, string> = {
  Behavioral: 'Behavioral',
  TechnicalCoding: 'Technical Coding',
  SystemDesign: 'System Design'
};

const TRACK_SUBTITLES: Record<string, string> = {
  Behavioral: 'STAR-based, role-aligned',
  TechnicalCoding: 'Live coding with hints',
  SystemDesign: 'Whiteboard mode'
};

@Component({
  selector: 'app-interview-dashboard',
  imports: [CommonModule, Card],
  templateUrl: './interview-dashboard.html',
  styleUrl: './interview-dashboard.css',
})
export class InterviewDashboard {
  history = input.required<InterviewHistoryItemDto[]>();
  lastSession = input<InterviewHistoryItemDto | null>(null);
  hasActiveInProgress = input<boolean>(false);
  selectedTrackFilter = input<string | null>(null);
  trackOptions = input.required<InterviewOptionItem[]>();
  trackCounts = input.required<Record<string, number>>();

  setupNew = output<void>();
  viewLastScorecard = output<void>();
  resumeActiveInterview = output<void>();
  selectTrackFilter = output<string>();
  clearTrackFilter = output<void>();
  loadScorecardById = output<number>();
  requestDeleteSession = output<{ sessionId: number; event: MouseEvent }>();

  filteredHistory = computed(() => {
    const filter = this.selectedTrackFilter();
    if (!filter) return this.history();
    return this.history().filter(item => item.track === filter);
  });

  trackLabel(value: string): string {
    return TRACK_LABELS[value] ?? value;
  }

  trackSubtitle(value: string): string {
    return TRACK_SUBTITLES[value] ?? '';
  }

  gradeClass(grade: string): string {
    if (grade === 'A' || grade === 'A-') return 'grade-a';
    if (grade === 'B+' || grade === 'B') return 'grade-b';
    if (grade === 'C') return 'grade-c';
    return 'grade-default';
  }

  gradeLabel(grade: string): string {
    if (grade === 'A' || grade === 'A-') return 'Excellent';
    if (grade === 'B+' || grade === 'B') return 'Good';
    if (grade === 'C') return 'Needs Work';
    return 'Unrated';
  }

  onViewLastScorecard(): void { this.viewLastScorecard.emit(); }
  onResumeActiveInterview(): void { this.resumeActiveInterview.emit(); }
  onSetupNew(): void { this.setupNew.emit(); }
  onSelectTrackFilter(trackValue: string): void { this.selectTrackFilter.emit(trackValue); }
  onClearTrackFilter(): void { this.clearTrackFilter.emit(); }
  onLoadScorecardById(sessionId: number): void { this.loadScorecardById.emit(sessionId); }
  onRequestDeleteSession(sessionId: number, event: MouseEvent): void {
    this.requestDeleteSession.emit({ sessionId, event });
  }
}
