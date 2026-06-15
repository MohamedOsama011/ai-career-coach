import { Component, input, output } from '@angular/core';
import { Card } from '../card/card';

@Component({
  selector: 'app-job-card',
  imports: [Card],
  template: `
    <app-card class="job-card-container">
      <div class="logo-area">
        <div class="company-logo">
          {{ logoInitials() || 'JB' }}
        </div>
      </div>
      
      <div class="info-area">
        <div class="match-score">{{ matchPercentage() }}% MATCH</div>
        <h3 class="job-title">{{ title() }}</h3>
        <div class="job-meta">
          <span class="company">{{ company() }}</span>
          <span class="dot">&bull;</span>
          <span class="location">
            <span class="material-icons location-icon">place</span>
            {{ location() }}
          </span>
        </div>
      </div>

      <div class="skills-area">
        @for (skill of skills(); track skill) {
          <span class="skill-tag">{{ skill }}</span>
        }
      </div>

      <div class="salary-area">
        {{ salary() }}
      </div>

      <div class="actions-area">
        <button 
          class="bookmark-btn" 
          [class.is-saved]="saved()" 
          (click)="saveToggled.emit(); $event.stopPropagation()"
          aria-label="Save Job"
        >
          <span class="material-icons">{{ saved() ? 'bookmark' : 'bookmark_border' }}</span>
        </button>
        <button class="apply-btn" (click)="applied.emit(); $event.stopPropagation()">
          Apply
        </button>
      </div>
    </app-card>
  `,
  styles: [`
    :host {
      display: block;
      margin-bottom: 16px;
    }
    .job-card-container {
      display: grid;
      grid-template-columns: 52px 2fr 1.5fr 1fr auto;
      align-items: center;
      gap: 24px;
      padding: 24px;
      background: #FFFFFF;
      border: 1px solid #E5E7EB;
      border-radius: 16px;
      transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .job-card-container:hover {
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.05);
      border-color: #D1D5DB;
      transform: translateY(-2px);
    }
    .company-logo {
      width: 52px;
      height: 52px;
      border: 1px solid #E5E7EB;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 14px;
      font-weight: 700;
      color: #4B5563;
      background: #FFFFFF;
      letter-spacing: 0.5px;
    }
    .info-area {
      display: flex;
      flex-direction: column;
      justify-content: center;
    }
    .match-score {
      font-size: 11px;
      font-weight: 700;
      color: #3B82F6;
      letter-spacing: 0.5px;
      margin-bottom: 6px;
      text-transform: uppercase;
    }
    .job-title {
      font-size: 18px;
      font-weight: 700;
      color: #111827;
      margin: 0 0 6px 0;
      line-height: 1.2;
    }
    .job-meta {
      font-size: 14px;
      color: #6B7280;
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .location-icon {
      font-size: 14px;
      vertical-align: middle;
      color: #9CA3AF;
      margin-right: 2px;
    }
    .dot {
      color: #9CA3AF;
    }
    .skills-area {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      justify-content: flex-start;
      align-items: center;
    }
    .skill-tag {
      background: #F3F4F6;
      border-radius: 8px;
      padding: 6px 12px;
      font-size: 13px;
      color: #374151;
      font-weight: 500;
      line-height: 1;
    }
    .salary-area {
      font-size: 16px;
      font-weight: 600;
      color: #111827;
      text-align: left;
    }
    .actions-area {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .bookmark-btn {
      background: none;
      border: 1px solid #E5E7EB;
      border-radius: 8px;
      width: 38px;
      height: 38px;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      color: #6B7280;
      transition: all 0.2s ease;
    }
    .bookmark-btn:hover {
      background: #F9FAFB;
      border-color: #D1D5DB;
      color: #374151;
    }
    .bookmark-btn.is-saved {
      color: #2563EB;
      border-color: #BFDBFE;
      background: #EFF6FF;
    }
    .apply-btn {
      background: #2563EB;
      color: #FFFFFF;
      border: none;
      border-radius: 8px;
      padding: 8px 22px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s ease;
    }
    .apply-btn:hover {
      background: #1D4ED8;
    }

    @media (max-width: 1024px) {
      .job-card-container {
        grid-template-columns: 52px 1fr 1fr;
        gap: 16px;
      }
      .skills-area {
        grid-column: 2 / span 2;
      }
      .salary-area {
        grid-column: 2;
      }
      .actions-area {
        grid-column: 3;
        justify-content: flex-end;
      }
    }

    @media (max-width: 640px) {
      .job-card-container {
        grid-template-columns: 1fr;
        gap: 16px;
      }
      .logo-area, .info-area, .skills-area, .salary-area, .actions-area {
        grid-column: 1;
      }
      .actions-area {
        justify-content: space-between;
      }
      .apply-btn {
        flex: 1;
      }
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
  logoInitials = input<string>('');
  saved = input<boolean>(false);
  
  applied = output<void>();
  saveToggled = output<void>();
}

