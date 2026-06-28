import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Card } from '../../../shared/components/card/card';
import { Badge, BadgeVariant } from '../../../shared/components/badge/badge';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import {
  InterviewHistoryItemDto,
  InterviewScorecardDto,
  InterviewSessionDto
} from '../../../core/models/interview.model';

const TRACK_LABELS: Record<string, string> = {
  Behavioral: 'Behavioral',
  TechnicalCoding: 'Technical Coding',
  SystemDesign: 'System Design'
};

@Component({
  selector: 'app-interview-scorecard',
  imports: [CommonModule, Card, Badge, StatCard],
  templateUrl: './interview-scorecard.html',
  styleUrl: './interview-scorecard.css',
})
export class InterviewScorecardComponent {
  scorecard = input.required<InterviewScorecardDto>();
  session = input<InterviewSessionDto | null>(null);
  viewedScorecardMeta = input<InterviewHistoryItemDto | null>(null);
  converting = input<boolean>(false);
  convertError = input<string | null>(null);

  newSession = output<void>();
  convertToRoadmap = output<void>();
  printScorecard = output<void>();

  trackLabel(value: string): string {
    return TRACK_LABELS[value] ?? value;
  }

  ratingVariant(rating: string): BadgeVariant {
    if (rating === 'Strong') return 'success';
    if (rating === 'Adequate') return 'warning';
    if (rating === 'Weak') return 'danger';
    return 'default';
  }

  onNewSession(): void { this.newSession.emit(); }
  onConvertToRoadmap(): void { this.convertToRoadmap.emit(); }
  onPrintScorecard(): void { this.printScorecard.emit(); }
}
