import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe, KeyValuePipe } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { AuditLogEntry } from '../../../core/models/admin.model';

@Component({
  selector: 'app-audit-log',
  imports: [DatePipe, KeyValuePipe],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.css',
})
export class AuditLog implements OnInit {
  private adminService = inject(AdminService);

  logs = signal<AuditLogEntry[]>([]);
  totalCount = signal(0);
  page = signal(1);
  pageSize = signal(20);
  loading = signal(false);
  error = signal<string | null>(null);
  actionFilter = signal<string>('all');
  adminFilter = signal<string>('all');

  hasNextPage = computed(() => this.page() * this.pageSize() < this.totalCount());
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  rangeStart = computed(() => (this.page() - 1) * this.pageSize() + 1);
  rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.totalCount()));

  adminNames = computed(() => {
    const names = new Map<string, string>();
    for (const log of this.logs()) {
      if (log.adminUserId && !names.has(log.adminUserId)) {
        names.set(log.adminUserId, log.adminUserName);
      }
    }
    return names;
  });

  actionLabels: Record<string, string> = {
    clear_cache: 'Clear Cache',
    delete_user: 'Delete User',
    change_role: 'Change Role',
    delete_cv: 'Delete CV',
  };

  actionKeys = Object.keys(this.actionLabels);

  actionIcons: Record<string, string> = {
    clear_cache: 'cached',
    delete_user: 'person_remove',
    change_role: 'manage_accounts',
    delete_cv: 'description',
  };

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading.set(true);
    this.error.set(null);

    const params: { page: number; pageSize: number; action?: string; adminId?: string } = {
      page: this.page(),
      pageSize: this.pageSize(),
    };

    if (this.actionFilter() !== 'all') {
      params.action = this.actionFilter();
    }

    if (this.adminFilter() !== 'all') {
      params.adminId = this.adminFilter();
    }

    this.adminService.getAuditLogs(params).subscribe({
      next: res => {
        this.logs.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load audit logs');
        this.loading.set(false);
      },
    });
  }

  setActionFilter(action: string): void {
    this.actionFilter.set(action);
    this.page.set(1);
    this.loadLogs();
  }

  setAdminFilter(adminId: string): void {
    this.adminFilter.set(adminId);
    this.page.set(1);
    this.loadLogs();
  }

  onAdminFilterChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.setAdminFilter(target.value);
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.loadLogs();
  }

  prevPage(): void {
    this.goToPage(this.page() - 1);
  }

  nextPage(): void {
    this.goToPage(this.page() + 1);
  }

  actionLabel(action: string): string {
    return this.actionLabels[action] ?? action.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
  }

  actionIcon(action: string): string {
    return this.actionIcons[action] ?? 'history';
  }
}
