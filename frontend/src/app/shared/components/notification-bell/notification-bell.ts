import { Component, inject, signal, computed, effect, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDto } from '../../../core/models/notification.model';

@Component({
  selector: 'app-notification-bell',
  imports: [],
  templateUrl: './notification-bell.html',
  styleUrl: './notification-bell.css',
})
export class NotificationBell implements OnDestroy {
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  unreadCount = signal(0);
  dropdownOpen = signal(false);
  recentNotifications = signal<NotificationDto[]>([]);
  loading = signal(false);

  truncate(text: string, max: number): string {
    return text.length > max ? text.substring(0, max) + '...' : text;
  }

  private pollTimer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    this.refresh();
    this.pollTimer = setInterval(() => this.refresh(), 60000);
  }

  ngOnDestroy(): void {
    if (this.pollTimer) clearInterval(this.pollTimer);
  }

  refresh(): void {
    this.notificationService.getUnreadCount().subscribe({
      next: (res) => this.unreadCount.set(res.count),
    });
  }

  toggleDropdown(): void {
    if (!this.dropdownOpen()) {
      this.openDropdown();
    } else {
      this.closeDropdown();
    }
  }

  openDropdown(): void {
    this.loading.set(true);
    this.dropdownOpen.set(true);
    this.notificationService.getNotifications(1, 5).subscribe({
      next: (res) => {
        this.recentNotifications.set(res.items);
        this.unreadCount.set(res.unreadCount);
      },
      complete: () => this.loading.set(false),
    });
  }

  closeDropdown(): void {
    this.dropdownOpen.set(false);
  }

  markRead(notification: NotificationDto): void {
    if (notification.isRead) return;
    this.notificationService.markAsRead(notification.id).subscribe({
      next: () => {
        notification.isRead = true;
        this.unreadCount.update((c) => Math.max(0, c - 1));
      },
    });
  }

  markAllRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.unreadCount.set(0);
        this.recentNotifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
      },
    });
  }

  viewAll(): void {
    this.closeDropdown();
  }
}
