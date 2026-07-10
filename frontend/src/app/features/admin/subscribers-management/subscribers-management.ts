import { Component, inject, OnInit, signal, computed, effect } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import * as Papa from 'papaparse';
import { UserSubscriptionService } from '../../../core/services/user-subscription.service';
import { AdminSubscriptionService } from '../../../core/services/admin-subscription.service';
import { UserSubscriptionDto } from '../../../core/models/payment.model';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

type StatusFilter = 'All' | 'Active' | 'Pending' | 'Cancelled' | 'Expired';

@Component({
  selector: 'app-subscribers-management',
  imports: [DatePipe, FormsModule, ConfirmModal],
  templateUrl: './subscribers-management.html',
  styleUrl: './subscribers-management.css',
})
export class SubscribersManagement implements OnInit {
  private userSubscriptionService = inject(UserSubscriptionService);
  private adminSubscriptionService = inject(AdminSubscriptionService);
  private router = inject(Router);
  private papa = Papa;

  allSubscribers = signal<UserSubscriptionDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  exporting = signal(false);

  statusFilter = signal<StatusFilter>('All');
  planFilter = signal<string>('All');
  searchTerm = signal<string>('');
  fromDate = signal<string>('');
  toDate = signal<string>('');

  openDropdownId = signal<number | null>(null);

  pendingCancelId = signal<number | null>(null);
  cancelling = signal(false);

  availablePlans = computed(() => {
    const names = new Set<string>();
    for (const s of this.allSubscribers()) {
      if (s.subscription?.name) names.add(s.subscription.name);
    }
    return Array.from(names).sort();
  });

  filteredSubscribers = computed(() => {
    const status = this.statusFilter();
    const plan = this.planFilter();
    const search = this.searchTerm().trim().toLowerCase();
    return this.allSubscribers().filter(s => {
      const statusOk = status === 'All' || s.status === status;
      const planOk = plan === 'All' || s.subscription?.name === plan;
      const searchOk = !search ||
        (s.user?.fullName?.toLowerCase().includes(search) ?? false) ||
        (s.user?.email?.toLowerCase().includes(search) ?? false);
      return statusOk && planOk && searchOk;
    });
  });

  totalCount = computed(() => this.allSubscribers().length);
  activeCount = computed(() => this.allSubscribers().filter(s => s.isActive).length);
  filteredCount = computed(() => this.filteredSubscribers().length);
  hasActiveFilters = computed(() =>
    this.statusFilter() !== 'All' ||
    this.planFilter() !== 'All' ||
    this.searchTerm().trim() !== '' ||
    this.fromDate() !== '' ||
    this.toDate() !== ''
  );

  pendingCancelUser = computed(() => {
    const id = this.pendingCancelId();
    return id ? this.allSubscribers().find(s => s.id === id) : null;
  });

  private searchDebounce: ReturnType<typeof setTimeout> | null = null;
  debouncedSearch = effect(() => {
    this.searchTerm();
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => this.loadSubscribers(), 300);
  });

  ngOnInit(): void {
    this.loadSubscribers();
  }

  loadSubscribers(): void {
    this.loading.set(true);
    this.error.set(null);

    const from = this.fromDate() ? new Date(this.fromDate()) : undefined;
    const to = this.toDate() ? new Date(this.toDate() + 'T23:59:59') : undefined;
    const search = this.searchTerm().trim() || undefined;

    this.userSubscriptionService.getAll(search, from, to).subscribe({
      next: (res) => {
        this.allSubscribers.set(Array.isArray(res.data) ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load subscribers.');
        this.loading.set(false);
      },
    });
  }

  resetFilters(): void {
    this.statusFilter.set('All');
    this.planFilter.set('All');
    this.searchTerm.set('');
    this.fromDate.set('');
    this.toDate.set('');
    this.loadSubscribers();
  }

  onDateChange(): void {
    this.loadSubscribers();
  }

  exportCsv(): void {
    if (this.filteredSubscribers().length === 0) return;
    this.exporting.set(true);

    const rows = this.filteredSubscribers().map(s => ({
      'User ID': s.userId,
      'Email': s.user?.email ?? '',
      'Full Name': s.user?.fullName ?? '',
      'Plan': s.subscription?.name ?? '',
      'Status': s.status,
      'Active': s.isActive ? 'Yes' : 'No',
      'Start Date': s.startDate ? new Date(s.startDate).toISOString().slice(0, 10) : '',
      'End Date': s.endDate ? new Date(s.endDate).toISOString().slice(0, 10) : '',
      'Created At': s.createdAt ? new Date(s.createdAt).toISOString().slice(0, 10) : '',
      'Last Payment Status': s.payments && s.payments.length > 0 ? s.payments[0].status : '',
    }));

    const csv = this.papa.unparse(rows);
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    const timestamp = new Date().toISOString().slice(0, 10);
    link.href = url;
    link.download = `subscribers-${timestamp}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    this.exporting.set(false);
  }

  toggleDropdown(id: number): void {
    this.openDropdownId.set(this.openDropdownId() === id ? null : id);
  }

  closeDropdown(): void {
    this.openDropdownId.set(null);
  }

  openDetail(id: number): void {
    this.closeDropdown();
    this.router.navigate(['/admin/subscribers', id]);
  }

  requestCancel(id: number): void {
    this.closeDropdown();
    this.pendingCancelId.set(id);
  }

  confirmCancel(): void {
    const id = this.pendingCancelId();
    if (id === null) return;
    this.cancelling.set(true);
    this.adminSubscriptionService.cancel(id, 'Cancelled by admin from subscribers list').subscribe({
      next: () => {
        this.cancelling.set(false);
        this.pendingCancelId.set(null);
        this.loadSubscribers();
      },
      error: () => {
        this.cancelling.set(false);
        this.pendingCancelId.set(null);
        this.error.set('Failed to cancel subscription.');
      },
    });
  }

  cancelCancel(): void {
    this.pendingCancelId.set(null);
  }
}
