import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { UserManagement } from '../../../core/models/admin.model';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-admin-users',
  imports: [DatePipe, ConfirmModal],
  templateUrl: './users.html',
  styleUrl: './users.css',
})
export class AdminUsers implements OnInit {
  private adminService = inject(AdminService);
  private router = inject(Router);

  users = signal<UserManagement[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  searchQuery = signal('');
  roleUpdatingId = signal<string | null>(null);
  deletingId = signal<string | null>(null);
  confirmAction = signal<{ type: 'change-role' | 'delete'; userId: string; userName: string; role?: string } | null>(null);

  filteredUsers = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.users();
    return this.users().filter(u =>
      u.fullName.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q)
    );
  });

  totalCount = computed(() => this.users().length);
  filteredCount = computed(() => this.filteredUsers().length);
  hasActiveFilters = computed(() => this.searchQuery().length > 0);

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.error.set(null);

    this.adminService.getUserManagement().subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load users.');
        this.loading.set(false);
      },
    });
  }

  viewUser(id: string): void {
    this.router.navigate(['/admin/users', id]);
  }

  requestChangeRole(user: UserManagement): void {
    const newRole = user.role === 'Admin' ? 'User' : 'Admin';
    this.confirmAction.set({
      type: 'change-role',
      userId: user.id,
      userName: user.fullName,
      role: newRole,
    });
  }

  requestDeleteUser(user: UserManagement): void {
    this.confirmAction.set({
      type: 'delete',
      userId: user.id,
      userName: user.fullName,
    });
  }

  closeConfirm(): void {
    this.confirmAction.set(null);
  }

  handleConfirm(): void {
    const action = this.confirmAction();
    if (!action) return;

    if (action.type === 'change-role') {
      this.roleUpdatingId.set(action.userId);
      this.adminService.changeRole(action.userId, action.role!).subscribe({
        next: () => {
          this.roleUpdatingId.set(null);
          this.confirmAction.set(null);
          this.loadUsers();
        },
        error: () => {
          this.roleUpdatingId.set(null);
          this.confirmAction.set(null);
          this.error.set('Failed to change role. Cannot remove the last admin.');
        },
      });
    } else if (action.type === 'delete') {
      this.deletingId.set(action.userId);
      this.adminService.deleteUser(action.userId).subscribe({
        next: () => {
          this.deletingId.set(null);
          this.confirmAction.set(null);
          this.loadUsers();
        },
        error: () => {
          this.deletingId.set(null);
          this.confirmAction.set(null);
          this.error.set('Failed to delete user. Cannot delete the last admin.');
        },
      });
    }
  }

  roleBadgeClass(role: string): string {
    return role === 'Admin' ? 'role-admin' : 'role-user';
  }
}
