import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { UserDetailDto, SubscriberSessionDto, SubscriberCvDto, SubscriberRoadmapDto, PaymentInvoiceDto } from '../../../core/models/admin.model';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-user-detail',
  imports: [DatePipe, ConfirmModal],
  templateUrl: './user-detail.html',
  styleUrl: './user-detail.css',
})
export class UserDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminService = inject(AdminService);

  detail = signal<UserDetailDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  activeTab = signal<'cvs' | 'interviews' | 'roadmaps' | 'payments'>('cvs');

  user = computed(() => this.detail()?.user);
  cvs = computed(() => this.detail()?.cVs ?? []);
  interviews = computed(() => this.detail()?.interviews);
  roadmaps = computed(() => this.detail()?.roadmaps ?? []);
  payments = computed(() => this.detail()?.payments ?? []);
  initials = computed(() => {
    const name = this.user()?.fullName ?? '';
    const parts = name.split(' ').filter(Boolean);
    return parts.length >= 2 ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase() : name.charAt(0).toUpperCase();
  });

  roleUpdating = signal(false);
  deleting = signal(false);
  confirmAction = signal<{ type: string; title: string; message: string; role?: string } | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Invalid user ID');
      return;
    }
    this.loadDetail(id);
  }

  loadDetail(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.adminService.getUserDetail(id).subscribe({
      next: (res) => {
        this.detail.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load user details.');
        this.loading.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin']);
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Active': case 'Paid': return 'status-active';
      case 'Completed': return 'status-completed';
      case 'Pending': return 'status-pending';
      case 'Failed': case 'Cancelled': case 'Expired': case 'Abandoned': return 'status-inactive';
      default: return '';
    }
  }

  requestChangeRole(newRole: string): void {
    const roleLabel = newRole.charAt(0).toUpperCase() + newRole.slice(1);
    this.confirmAction.set({
      type: 'change-role',
      title: 'Change User Role?',
      message: `Change this user's role to ${roleLabel}?`,
      role: newRole,
    });
  }

  requestDeleteUser(): void {
    this.confirmAction.set({
      type: 'delete',
      title: 'Delete User?',
      message: 'Are you sure you want to permanently delete this user? All their data will be removed. This action cannot be undone.',
    });
  }

  closeConfirm(): void {
    this.confirmAction.set(null);
  }

  handleConfirm(): void {
    const action = this.confirmAction();
    const userId = this.user()?.id;
    if (!action || !userId) return;

    if (action.type === 'change-role') {
      const newRole = action.role ?? 'User';
      this.roleUpdating.set(true);
      this.adminService.changeRole(userId, newRole).subscribe({
        next: () => {
          this.roleUpdating.set(false);
          this.confirmAction.set(null);
          this.loadDetail(userId);
        },
        error: () => {
          this.roleUpdating.set(false);
          this.confirmAction.set(null);
          this.error.set('Failed to change role. Cannot remove the last admin.');
        },
      });
    } else if (action.type === 'delete') {
      this.deleting.set(true);
      this.adminService.deleteUser(userId).subscribe({
        next: () => {
          this.deleting.set(false);
          this.confirmAction.set(null);
          this.router.navigate(['/admin']);
        },
        error: () => {
          this.deleting.set(false);
          this.confirmAction.set(null);
          this.error.set('Failed to delete user. Cannot delete the last admin.');
        },
      });
    }
  }

  switchTab(tab: 'cvs' | 'interviews' | 'roadmaps' | 'payments'): void {
    this.activeTab.set(tab);
  }
}
