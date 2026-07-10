import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ExtendSubscriptionRequest } from '../../../../core/models/payment.model';

@Component({
  selector: 'app-extend-modal',
  imports: [FormsModule],
  templateUrl: './extend-modal.html',
  styleUrl: './extend-modal.css',
})
export class ExtendModal {
  open = input(false);
  loading = input(false);
  confirm = output<ExtendSubscriptionRequest>();
  cancel = output<void>();

  additionalDays = signal(30);
  notes = signal('');

  handleConfirm(): void {
    if (this.additionalDays() < 1) return;
    this.confirm.emit({
      additionalDays: this.additionalDays(),
      notes: this.notes().trim() || undefined,
    });
  }

  getNewEndDate(): string {
    const date = new Date();
    date.setDate(date.getDate() + this.additionalDays());
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  handleBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.cancel.emit();
    }
  }

  handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !this.loading()) {
      this.handleConfirm();
    }
    if (event.key === 'Escape') {
      this.cancel.emit();
    }
  }
}
