import { Component, input, output } from '@angular/core';
import { Card } from '../card/card';
import { Badge } from '../badge/badge';

@Component({
  selector: 'app-job-card',
  imports: [Card, Badge],
  template: `
    <app-card class="job-card">
      <div class="job-match">
        <div class="match-circle" [style.background]="matchColor">
          <span>{{ matchPercentage() }}%</span>
        </div>
      </div>
      <div class="job-info">
        <h4 class="job-title">{{ title() }}</h4>
        <p class="job-company">{{ company() }}</p>
        <p class="job-meta">
          <span>{{ location() }}</span>
          @if (salary()) {
            <span class="dot">&middot;</span>
            <span>{{ salary() }}</span>
          }
        </p>
        <div class="job-skills">
          @for (skill of skills(); track skill) {
            <app-badge [text]="skill" variant="info" />
          }
        </div>
      </div>
      <button class="apply-btn" (click)="applied.emit()">Apply Now</button>
    </app-card>
  `,
  styles: [`
    .job-card {
      display: flex;
      gap: 16px;
      align-items: flex-start;
      cursor: pointer;
      transition: border-color 0.15s ease;
    }
    .job-card:hover {
      border-color: var(--brand-primary);
    }
    .job-match {
      flex-shrink: 0;
    }
    .match-circle {
      width: 56px;
      height: 56px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-size: 14px;
      font-weight: 700;
    }
    .job-info {
      flex: 1;
      min-width: 0;
    }
    .job-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--brand-text);
      margin: 0 0 4px;
    }
    .job-company {
      font-size: 14px;
      color: var(--brand-text-secondary);
      margin: 0 0 4px;
    }
    .job-meta {
      font-size: 13px;
      color: var(--brand-text-secondary);
      margin: 0 0 8px;
    }
    .dot {
      margin: 0 6px;
    }
    .job-skills {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
    }
    .apply-btn {
      flex-shrink: 0;
      padding: 8px 20px;
      border: 1px solid var(--brand-border);
      border-radius: 8px;
      background: white;
      color: var(--brand-text);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s ease;
    }
    .apply-btn:hover {
      background: var(--brand-primary);
      color: white;
      border-color: var(--brand-primary);
    }
  `],
})
export class JobCard {
  matchPercentage = input.required<number>();
  title = input.required<string>();
  company = input.required<string>();
  location = input.required<string>();
  salary = input<string>('');
  skills = input<string[]>([]);
  applied = output<void>();

  get matchColor(): string {
    const v = this.matchPercentage();
    if (v >= 80) return 'var(--brand-success)';
    if (v >= 60) return 'var(--brand-warning)';
    return 'var(--brand-danger)';
  }
}
