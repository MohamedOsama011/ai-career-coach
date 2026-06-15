import { Component, input } from '@angular/core';

@Component({
  selector: 'app-progress-bar',
  imports: [],
  template: `
    <div class="progress-bar">
      @if (label()) {
        <div class="progress-header">
          <span class="progress-label">{{ label() }}</span>
          <span class="progress-value">{{ value() }}%</span>
        </div>
      }
      <div class="progress-track">
        <div
          class="progress-fill"
          [style.width.%]="value()"
          [style.background]="color()"
        ></div>
      </div>
    </div>
  `,
  styles: [`
    .progress-bar {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .progress-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .progress-label {
      font-size: 14px;
      color: var(--brand-text-secondary);
    }
    .progress-value {
      font-size: 14px;
      font-weight: 600;
      color: var(--brand-text);
    }
    .progress-track {
      height: 8px;
      background: #F3F4F6;
      border-radius: 999px;
      overflow: hidden;
    }
    .progress-fill {
      height: 100%;
      border-radius: 999px;
      transition: width 0.4s ease;
    }
  `],
})
export class ProgressBar {
  value = input.required<number>();
  label = input<string>('');
  color = input<string>('var(--brand-primary)');
}
