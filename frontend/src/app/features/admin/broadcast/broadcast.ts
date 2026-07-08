import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-broadcast',
  imports: [FormsModule, ConfirmModal],
  templateUrl: './broadcast.html',
  styleUrl: './broadcast.css',
})
export class Broadcast {
  private adminService = inject(AdminService);

  title = signal('');
  body = signal('');
  targetType = signal<'all' | 'plan' | 'user'>('all');
  targetValue = signal('');
  notificationType = signal<'info' | 'warning' | 'success' | 'broadcast'>('broadcast');
  sending = signal(false);
  sent = signal(false);
  error = signal('');
  confirmOpen = signal(false);

  isFormValid = computed(() => this.title().trim().length > 0 && this.body().trim().length > 0);

  showPreview = signal(false);

  get previewTitle(): string {
    return this.title() || 'Your notification title';
  }

  get previewBody(): string {
    return this.body() || 'Your notification message will appear here.';
  }

  get typeIcon(): string {
    switch (this.notificationType()) {
      case 'success': return 'check_circle';
      case 'warning': return 'warning';
      case 'broadcast': return 'campaign';
      default: return 'info';
    }
  }

  setNotificationType(value: string): void {
    if (value === 'info' || value === 'warning' || value === 'success' || value === 'broadcast') {
      this.notificationType.set(value);
    }
  }

  openConfirm(): void {
    this.confirmOpen.set(true);
  }

  send(): void {
    this.sending.set(true);
    this.error.set('');
    this.confirmOpen.set(false);

    this.adminService.broadcast({
      targetType: this.targetType(),
      targetValue: this.targetType() !== 'all' ? this.targetValue() || undefined : undefined,
      title: this.title(),
      body: this.body(),
      type: this.notificationType(),
    }).subscribe({
      next: () => {
        this.sending.set(false);
        this.sent.set(true);
        this.title.set('');
        this.body.set('');
        this.targetValue.set('');
        setTimeout(() => this.sent.set(false), 5000);
      },
      error: (err) => {
        this.sending.set(false);
        this.error.set(err.error?.message || err.message || 'Failed to send broadcast');
      },
    });
  }
}
