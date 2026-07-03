import { Component, input, output } from '@angular/core';
import { Card } from '../card/card';

@Component({
  selector: 'app-job-card',
  imports: [Card],
  template: `
    <app-card class="job-card-container">
      <div class="logo-area">
        @if (companyLogoUrl()) {
          <img [src]="companyLogoUrl()" alt="" class="company-logo-img" width="48" height="48" />
        } @else {
          <div class="company-logo">
            {{ logoInitials() || 'JB' }}
          </div>
        }
      </div>
      
      <div class="info-area">
        <h3 class="job-title">{{ title() }}</h3>
        <div class="job-meta">
          <span class="company">{{ company() }}</span>
          <span class="dot">&bull;</span>
          <span class="location">
            <span class="material-icons location-icon">place</span>
            {{ location() }}
          </span>
        </div>
        <div class="job-badges">
          @if (isRemote()) {
            <span class="remote-badge">Remote</span>
          }
          @if (source()) {
            <span class="source-chip">{{ source() }}</span>
          }
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
      animation: cardFadeIn 0.4s ease both;
      animation-delay: calc(var(--i, 0) * 0.08s);
    }
    .job-card-container {
      display: grid;
      grid-template-columns: 52px 2fr 1.5fr 1fr auto;
      align-items: center;
      gap: 24px;
      padding: 24px;
      background: var(--brand-card);
      border: 1px solid var(--brand-border);
      border-radius: 16px;
      transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .job-card-container:hover {
      box-shadow: var(--shadow-sm);
      border-color: var(--brand-border);
      transform: translateY(-2px);
    }
    .company-logo {
      width: 52px;
      height: 52px;
      border: 1px solid var(--brand-border);
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 14px;
      font-weight: 700;
      color: var(--brand-text-secondary);
      background: var(--brand-card);
      letter-spacing: 0.5px;
    }
    .company-logo-img {
      width: 48px;
      height: 48px;
      border-radius: 10px;
      object-fit: contain;
    }
    .info-area {
      display: flex;
      flex-direction: column;
      justify-content: center;
    }
    .job-title {
      font-size: 18px;
      font-weight: 700;
      color: var(--brand-text);
      margin: 0 0 6px 0;
      line-height: 1.2;
    }
    .job-meta {
      font-size: 14px;
      color: var(--brand-text-secondary);
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .job-badges {
      display: flex;
      gap: 6px;
      margin-top: 6px;
    }

    .remote-badge {
      display: inline-block;
      padding: 2px 8px;
      background: var(--brand-success-bg);
      color: var(--brand-success);
      font-size: 11px;
      font-weight: 600;
      border-radius: 9999px;
    }

    .source-chip {
      display: inline-block;
      padding: 2px 8px;
      background: var(--brand-bg);
      color: var(--brand-text-secondary);
      font-size: 11px;
      font-weight: 600;
      border-radius: 9999px;
    }
    .location-icon {
      font-size: 14px;
      vertical-align: middle;
      color: var(--brand-text-secondary);
      margin-right: 2px;
    }
    .dot {
      color: var(--brand-text-secondary);
    }
    .skills-area {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      justify-content: flex-start;
      align-items: center;
    }
    .skill-tag {
      background: var(--brand-bg);
      border-radius: 6px;
      padding: 4px 8px;
      font-size: 12px;
      color: var(--brand-text);
      font-weight: 500;
      line-height: 1;
    }
    .salary-area {
      font-size: 16px;
      font-weight: 600;
      color: var(--brand-text);
      text-align: left;
    }
    .actions-area {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .bookmark-btn {
      background: none;
      border: 1px solid var(--brand-border);
      border-radius: 8px;
      width: 38px;
      height: 38px;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      color: var(--brand-text-secondary);
      transition: all 0.2s ease;
    }
    .bookmark-btn:hover {
      background: var(--brand-card-hover);
      border-color: var(--brand-border);
      color: var(--brand-text);
    }
    .bookmark-btn.is-saved {
      color: var(--brand-primary);
      border-color: var(--brand-primary-bg);
      background: var(--brand-primary-bg);
    }
    .apply-btn {
      background: var(--brand-primary);
      color: var(--brand-on-primary);
      border: none;
      border-radius: 8px;
      padding: 8px 22px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s ease;
    }
    .apply-btn:hover {
      background: var(--brand-primary-hover);
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

    @keyframes cardFadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
  `],
})
export class JobCard {
  title = input.required<string>();
  company = input.required<string>();
  location = input.required<string>();
  salary = input<string>('');
  skills = input<string[]>([]);
  logoInitials = input<string>('');
  companyLogoUrl = input<string | undefined>('');
  saved = input<boolean>(false);
  isRemote = input<boolean>(false);
  source = input<string | undefined>(undefined);

  applied = output<void>();
  saveToggled = output<void>();
}

