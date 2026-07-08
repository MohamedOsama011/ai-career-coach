import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { InterviewSessionAdminDto } from '../../../core/models/admin.model';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

type StatusFilter = 'All' | 'Active' | 'Completed' | 'Abandoned';

@Component({
  selector: 'app-interview-admin',
  imports: [DatePipe, FormsModule, ConfirmModal],
  templateUrl: './interview-admin.html',
  styleUrl: './interview-admin.css',
})
export class InterviewAdmin implements OnInit {
  private adminService = inject(AdminService);

  sessions = signal<InterviewSessionAdminDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = signal(0);

  statusFilter = signal<StatusFilter>('All');
  trackFilter = signal<string>('All');
  fromDate = signal<string>('');
  toDate = signal<string>('');

  pendingDeleteId = signal<number | null>(null);
  deleting = signal(false);

  pendingAbortId = signal<number | null>(null);
  aborting = signal(false);

  totalItems = computed(() => this.totalCount());
  hasPreviousPage = computed(() => this.page() > 1);
  hasNextPage = computed(() => this.page() < this.totalPages());
  hasActiveFilters = computed(() =>
    this.statusFilter() !== 'All' ||
    this.trackFilter() !== 'All' ||
    this.fromDate() !== '' ||
    this.toDate() !== ''
  );

  pendingDeleteSession = computed<InterviewSessionAdminDto | null>(() => {
    const id = this.pendingDeleteId();
    if (id === null) return null;
    return this.sessions().find(s => s.id === id) ?? null;
  });

  pendingAbortSession = computed<InterviewSessionAdminDto | null>(() => {
    const id = this.pendingAbortId();
    if (id === null) return null;
    return this.sessions().find(s => s.id === id) ?? null;
  });

  ngOnInit(): void {
    this.loadSessions();
  }

  loadSessions(): void {
    this.loading.set(true);
    this.error.set(null);

    const status = this.statusFilter() !== 'All' ? this.statusFilter() : undefined;
    const track = this.trackFilter() !== 'All' ? this.trackFilter() : undefined;
    const from = this.fromDate() || undefined;
    const to = this.toDate() || undefined;

    this.adminService.getInterviewSessions({
      page: this.page(),
      pageSize: this.pageSize(),
      status,
      track,
      from,
      to,
    }).subscribe({
      next: (res) => {
        this.sessions.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load interview sessions.');
        this.loading.set(false);
      },
    });
  }

  onFilterChange(): void {
    this.page.set(1);
    this.loadSessions();
  }

  resetFilters(): void {
    this.statusFilter.set('All');
    this.trackFilter.set('All');
    this.fromDate.set('');
    this.toDate.set('');
    this.page.set(1);
    this.loadSessions();
  }

  goToPage(p: number): void {
    this.page.set(p);
    this.loadSessions();
  }

  statusClass(s: string): string {
    switch (s) {
      case 'Active': return 'status-active';
      case 'Completed': return 'status-completed';
      case 'Abandoned': return 'status-abandoned';
      default: return '';
    }
  }

  canAbort(s: InterviewSessionAdminDto): boolean {
    return s.status === 'Active';
  }

  requestDelete(id: number): void {
    this.pendingDeleteId.set(id);
  }

  cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  confirmDelete(): void {
    const id = this.pendingDeleteId();
    if (id === null) return;

    this.deleting.set(true);
    const snapshot = this.sessions();
    this.sessions.update(list => list.filter(s => s.id !== id));

    this.adminService.deleteInterviewSession(id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDeleteId.set(null);
      },
      error: (err) => {
        this.deleting.set(false);
        this.pendingDeleteId.set(null);
        this.sessions.set(snapshot);
        this.error.set(err?.message ?? 'Delete failed');
      },
    });
  }

  requestAbort(id: number): void {
    this.pendingAbortId.set(id);
  }

  cancelAbort(): void {
    this.pendingAbortId.set(null);
  }

  confirmAbort(): void {
    const id = this.pendingAbortId();
    if (id === null) return;

    this.aborting.set(true);
    const snapshot = this.sessions();
    this.sessions.update(list => list.map(s => s.id === id ? { ...s, status: 'Abandoned' } : s));

    this.adminService.abortInterviewSession(id).subscribe({
      next: () => {
        this.aborting.set(false);
        this.pendingAbortId.set(null);
      },
      error: (err) => {
        this.aborting.set(false);
        this.pendingAbortId.set(null);
        this.sessions.set(snapshot);
        this.error.set(err?.message ?? 'Abort failed');
      },
    });
  }

  selectStatus(status: string): void {
    this.statusFilter.set(status as StatusFilter);
    this.onFilterChange();
  }

  deleteMessage(name: string): string {
    return `Are you sure you want to delete the interview session for "${name}"? This will also remove all messages and scorecards.`;
  }

  abortMessage(name: string): string {
    return `Are you sure you want to abort the active interview session for "${name}"? The candidate will lose access.`;
  }
}
