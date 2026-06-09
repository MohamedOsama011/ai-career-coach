import { Component, input, output, ViewEncapsulation } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export type ButtonVariant = 'primary' | 'stroked' | 'danger';

@Component({
  selector: 'app-button',
  encapsulation: ViewEncapsulation.None,
  imports: [MatButtonModule, MatProgressSpinnerModule],
  template: `
    @if (variant() === 'primary') {
      <button
        mat-flat-button
        color="primary"
        class="app-btn app-btn-primary"
        [disabled]="disabled() || loading()"
        (click)="clicked.emit()"
      >
        @if (loading()) {
          <mat-spinner diameter="16" />
        }
        <ng-content />
      </button>
    } @else if (variant() === 'stroked') {
      <button
        mat-stroked-button
        color="primary"
        class="app-btn app-btn-stroked"
        [disabled]="disabled() || loading()"
        (click)="clicked.emit()"
      >
        @if (loading()) {
          <mat-spinner diameter="16" />
        }
        <ng-content />
      </button>
    } @else {
      <button
        mat-flat-button
        class="app-btn app-btn-danger"
        [disabled]="disabled() || loading()"
        (click)="clicked.emit()"
      >
        @if (loading()) {
          <mat-spinner diameter="16" />
        }
        <ng-content />
      </button>
    }
  `,
  styles: [`
    .app-btn { display: inline-flex !important; align-items: center !important; gap: 8px !important; }
    .app-btn mat-spinner circle { stroke: currentColor; }

    .app-btn-primary {
      --mat-filled-button-container-color: #2563EB !important;
      --mat-filled-button-label-text-color: white !important;
      --mat-filled-button-hover-state-layer-opacity: 0.08 !important;
    }
    .app-btn-primary:hover:not([disabled]) {
      --mat-filled-button-container-color: #1D4ED8 !important;
    }

    .app-btn-stroked {
      --mat-outlined-button-outline-color: #2563EB !important;
      --mat-outlined-button-label-text-color: #2563EB !important;
      --mat-outlined-button-hover-state-layer-opacity: 0.08 !important;
    }
    .app-btn-stroked:hover:not([disabled]) {
      --mat-outlined-button-outline-color: #1D4ED8 !important;
      --mat-outlined-button-label-text-color: #1D4ED8 !important;
    }

    .app-btn-danger {
      --mat-filled-button-container-color: #EF4444 !important;
      --mat-filled-button-label-text-color: white !important;
    }
    .app-btn-danger:hover:not([disabled]) {
      --mat-filled-button-container-color: #DC2626 !important;
    }
  `],
})
export class Button {
  variant = input<ButtonVariant>('primary');
  disabled = input(false);
  loading = input(false);
  clicked = output<void>();
}
