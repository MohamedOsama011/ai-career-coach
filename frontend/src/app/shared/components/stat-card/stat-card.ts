import { Component, input } from '@angular/core';
import { Card } from '../card/card';

@Component({
  selector: 'app-stat-card',
  imports: [Card],
  template: `
    <app-card class="stat-card">
      <div class="stat-header">
        <span class="material-icons stat-icon" [style.color]="color()">{{ icon() }}</span>
        <span class="stat-label">{{ label() }}</span>
      </div>
      <div class="stat-value">{{ value() }}</div>
      @if (trendText()) {
        <div class="stat-trend">
          <span class="material-icons trend-icon" [class.trend-up]="trendUp()" [class.trend-down]="!trendUp()">
            {{ trendUp() ? 'trending_up' : 'trending_down' }}
          </span>
          <span>{{ trendText() }}</span>
        </div>
      }
    </app-card>
  `,
  styles: [`
    .stat-card {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .stat-header {
      display: flex;
      align-items: center;
      margin-bottom: 8px;
      gap: 8px;
    }
    .stat-icon {
      font-size: 24px;
    }
    .stat-label {
      font-size: 14px;
      color: var(--brand-text-secondary);
      font-weight: 500;
    }
    .stat-value {
      font-size: 32px;
      font-weight: 700;
      color: var(--brand-text);
    }
    .stat-trend {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 14px;
      color: var(--brand-text-secondary);
    }
    .trend-icon {
      font-size: 18px;
    }
    .trend-up { color: var(--brand-success); }
    .trend-down { color: var(--brand-danger); }
  `],
})
export class StatCard {
  label = input.required<string>();
  value = input.required<string>();
  icon = input<string>('insights');
  color = input<string>('var(--brand-primary)');
  trendText = input<string>('');
  trendUp = input<boolean>(true);
}
