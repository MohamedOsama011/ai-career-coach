import { Component, input } from '@angular/core';

export type BadgeVariant = 'default' | 'success' | 'warning' | 'danger' | 'info';

@Component({
  selector: 'app-badge',
  imports: [],
  template: `<span class="badge badge-{{ variant() }}">{{ text() }}</span>`,
  styles: [`
    .badge {
      display: inline-flex;
      align-items: center;
      padding: 4px 12px;
      border-radius: 999px;
      font-size: 12px;
      font-weight: 600;
      line-height: 1.4;
      white-space: nowrap;
    }
    .badge-default {
      background: #F3F4F6;
      color: #6B7280;
    }
    .badge-success {
      background: #DCFCE7;
      color: #16A34A;
    }
    .badge-warning {
      background: #FEF3C7;
      color: #D97706;
    }
    .badge-danger {
      background: #FEE2E2;
      color: #DC2626;
    }
    .badge-info {
      background: #DBEAFE;
      color: #2563EB;
    }
  `],
})
export class Badge {
  text = input.required<string>();
  variant = input<BadgeVariant>('default');
}
