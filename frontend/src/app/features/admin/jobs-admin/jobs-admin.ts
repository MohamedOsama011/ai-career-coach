import { Component, inject, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { JobsService } from '../../../core/services/jobs.service';
import { AdminService } from '../../../core/services/admin.service';
import { Job, SyncStatusDto, UpdateJobDto } from '../../../core/models/job.model';
import { SyncLogDto } from '../../../core/models/admin.model';
import { JobsAdminTable } from './jobs-admin-table/jobs-admin-table';
import { JobFormModal } from './job-form-modal/job-form-modal';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-jobs-admin',
  imports: [JobsAdminTable, JobFormModal, ConfirmModal, DatePipe],
  templateUrl: './jobs-admin.html',
  styleUrl: './jobs-admin.css',
})
export class JobsAdmin implements OnInit, OnDestroy {
  private jobsService = inject(JobsService);
  private adminService = inject(AdminService);
  private _syncPollTimer: ReturnType<typeof setInterval> | null = null;

  jobs = signal<Job[]>([]);
  syncStatus = signal<SyncStatusDto | null>(null);
  syncLogs = signal<SyncLogDto[]>([]);

  loading = signal<boolean>(false);
  syncing = signal<boolean>(false);

  editingJob = signal<Job | null>(null);
  showAddModal = signal<boolean>(false);

  pendingDeleteId = signal<number | null>(null);
  deleting = signal<boolean>(false);
  error = signal<string | null>(null);
  syncMessage = signal<string | null>(null);

  totalJobs = computed(() => this.jobs().length);
  showHistory = signal(false);
  syncLogsLoading = signal(false);

  pendingDeleteJob = computed<Job | null>(() => {
    const id = this.pendingDeleteId();
    if (id === null) return null;
    return this.jobs().find(j => j.id === id) ?? null;
  });

  ngOnInit(): void {
    this.loadJobs();
    this.loadSyncStatus();
  }

  ngOnDestroy(): void {
    this.clearSyncPoll();
  }

  private clearSyncPoll(): void {
    if (this._syncPollTimer !== null) {
      clearInterval(this._syncPollTimer);
      this._syncPollTimer = null;
    }
  }

  loadJobs(): void {
    this.loading.set(true);
    this.error.set(null);
    this.jobsService.getJobs(1, 1000).subscribe({
      next: (result) => {
        this.jobs.set(result.jobs);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.message ?? 'Failed to load jobs');
        this.loading.set(false);
      }
    });
  }

  loadSyncStatus(): void {
    this.jobsService.getSyncStatus().subscribe({
      next: (status) => this.syncStatus.set(status),
      error: () => this.syncStatus.set(null)
    });
  }

  loadSyncLogs(): void {
    this.syncLogsLoading.set(true);
    this.adminService.getSyncLogs(50).subscribe({
      next: (logs) => {
        this.syncLogs.set(logs);
        this.syncLogsLoading.set(false);
      },
      error: () => this.syncLogsLoading.set(false),
    });
  }

  toggleHistory(): void {
    this.showHistory.update(v => !v);
    if (this.showHistory() && this.syncLogs().length === 0) {
      this.loadSyncLogs();
    }
  }

  triggerSync(): void {
    this.clearSyncPoll();
    this.syncing.set(true);
    this.error.set(null);
    this.syncMessage.set('Sync in progress...');

    const beforeLastSync = this.syncStatus()?.lastSyncAt;

    this.jobsService.syncJobs().subscribe({
      next: () => {
        this.syncing.set(false);
        this.pollForSyncCompletion(beforeLastSync);
      },
      error: (err) => {
        this.syncing.set(false);
        this.syncMessage.set(null);
        this.error.set(err?.message ?? 'Sync failed');
      }
    });
  }

  private pollForSyncCompletion(beforeLastSync: string | undefined): void {
    let attempts = 0;
    const maxAttempts = 36; // 36 × 5s = 3 min timeout

    this._syncPollTimer = setInterval(() => {
      attempts++;

      this.jobsService.getSyncStatus().subscribe({
        next: (status) => {
          this.syncStatus.set(status);

          if (status.lastSyncAt && status.lastSyncAt !== beforeLastSync) {
            this.clearSyncPoll();
            this.loadJobs();
            this.loadSyncLogs();
            this.syncMessage.set('Sync complete! New jobs have been added.');
            setTimeout(() => this.syncMessage.set(null), 3000);
          } else if (attempts >= maxAttempts) {
            this.clearSyncPoll();
            this.syncMessage.set('Sync is taking longer than expected. Check sync history for results.');
            setTimeout(() => this.syncMessage.set(null), 5000);
          }
        },
        error: () => {
          // keep polling on transient errors
        }
      });
    }, 5000);
  }

  openAdd(): void {
    this.editingJob.set(null);
    this.showAddModal.set(true);
  }

  openEdit(job: Job): void {
    this.editingJob.set(job);
    this.showAddModal.set(true);
  }

  closeModal(): void {
    this.showAddModal.set(false);
    this.editingJob.set(null);
  }

  saveJob(dto: UpdateJobDto): void {
    const editing = this.editingJob();
    if (editing) {
      this.jobsService.updateJob(editing.id, dto).subscribe({
        next: () => {
          this.closeModal();
          this.loadJobs();
        },
        error: (err) => this.error.set(err?.message ?? 'Update failed')
      });
    } else {
      const createDto = {
        title: dto.title,
        company: dto.company,
        description: dto.description,
        requiredSkills: dto.requiredSkills,
        location: dto.location,
        salary: dto.salary,
        companyLogoUrl: dto.companyLogoUrl,
        source: 'Manual',
        isRemote: dto.isRemote ?? false,
        externalUrl: dto.externalUrl
      };
      this.jobsService.createJob(createDto).subscribe({
        next: () => {
          this.closeModal();
          this.loadJobs();
        },
        error: (err) => this.error.set(err?.message ?? 'Create failed')
      });
    }
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
    const snapshot = this.jobs();
    this.jobs.update(list => list.filter(j => j.id !== id));

    this.jobsService.deleteJob(id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDeleteId.set(null);
      },
      error: (err) => {
        this.deleting.set(false);
        this.pendingDeleteId.set(null);
        this.jobs.set(snapshot);
        this.error.set(err?.message ?? 'Delete failed');
      }
    });
  }

  formatDuration(ms: number): string {
    if (ms < 1000) return `${ms}ms`;
    const seconds = Math.floor(ms / 1000);
    if (seconds < 60) return `${seconds}s`;
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${minutes}m ${secs}s`;
  }

  syncStatusClass(status: string): string {
    switch (status) {
      case 'Success': return 'status-success';
      case 'Warning': return 'status-warning';
      case 'Failed': return 'status-failed';
      default: return '';
    }
  }
}
