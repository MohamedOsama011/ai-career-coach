import { Component, input, output, ViewEncapsulation } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export type ButtonVariant = 'primary' | 'stroked' | 'danger' | 'success'| 'warning'| 'info';

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
      </button> }
@else if (variant() === 'success') {
  <button
    mat-flat-button
    class="app-btn app-btn-success"
    [disabled]="disabled() || loading()"
    (click)="clicked.emit()"
  >
    @if (loading()) {
      <mat-spinner diameter="16" />
    } @else {
      {{ label() }}
    }
  </button>
}

@else if (variant() === 'warning') {
  <button
    mat-flat-button
    class="app-btn app-btn-warning"
    [disabled]="disabled() || loading()"
    (click)="clicked.emit()"
  >
    @if (loading()) {
      <mat-spinner diameter="16" />
    } @else {
      {{ label() }}
    }
  </button>
}

@else if (variant() === 'info') {
  <button
    mat-flat-button
    class="app-btn app-btn-info"
    [disabled]="disabled() || loading()"
    (click)="clicked.emit()"
  >
    @if (loading()) {
      <mat-spinner diameter="16" />
    } @else {
      {{ label() }}
    }
  </button>
}

     @else {
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

.app-btn-success {
  --mat-filled-button-container-color: #16A34A !important;
  --mat-filled-button-label-text-color: #fff !important;
}

.app-btn-success:hover:not([disabled]) {
  --mat-filled-button-container-color: #15803D !important;
}

.app-btn-warning {
  --mat-filled-button-container-color: #F59E0B !important;
  --mat-filled-button-label-text-color: #fff !important;
}

.app-btn-warning:hover:not([disabled]) {
  --mat-filled-button-container-color: #D97706 !important;
}

.app-btn-info {
  --mat-filled-button-container-color: #2563EB !important;
  --mat-filled-button-label-text-color: white !important;


}

.app-btn-info:hover:not([disabled]) {
  --mat-filled-button-container-color: #1D4ED8 !important;
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

  label = input<string>('');
}
