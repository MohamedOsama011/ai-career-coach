import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { JobsService } from '../../../core/services/jobs.service';
import { Job, SyncResultDto, SyncStatusDto, UpdateJobDto } from '../../../core/models/job.model';
import { JobsAdminTable } from './jobs-admin-table/jobs-admin-table';
import { JobFormModal } from './job-form-modal/job-form-modal';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-jobs-admin',
  imports: [JobsAdminTable, JobFormModal, ConfirmModal, DatePipe],
  templateUrl: './jobs-admin.html',
  styleUrl: './jobs-admin.css',
})
export class JobsAdmin implements OnInit {
  private jobsService = inject(JobsService);

  jobs = signal<Job[]>([]);
  syncStatus = signal<SyncStatusDto | null>(null);

  loading = signal<boolean>(false);
  syncing = signal<boolean>(false);

  editingJob = signal<Job | null>(null);
  showAddModal = signal<boolean>(false);

  pendingDeleteId = signal<number | null>(null);
  deleting = signal<boolean>(false);
  error = signal<string | null>(null);

  totalJobs = computed(() => this.jobs().length);

  pendingDeleteJob = computed<Job | null>(() => {
    const id = this.pendingDeleteId();
    if (id === null) return null;
    return this.jobs().find(j => j.id === id) ?? null;
  });

  ngOnInit(): void {
    this.loadJobs();
    this.loadSyncStatus();
  }

  loadJobs(): void {
    this.loading.set(true);
    this.error.set(null);
    this.jobsService.getJobs().subscribe({
      next: (jobs) => {
        this.jobs.set(jobs);
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

  triggerSync(): void {
    this.syncing.set(true);
    this.error.set(null);
    this.jobsService.syncJobs().subscribe({
      next: (result: SyncResultDto) => {
        this.syncing.set(false);
        this.loadJobs();
        this.loadSyncStatus();
        if (result.errors > 0) {
          this.error.set(`Sync completed with ${result.errors} error(s). Fetched ${result.fetched}, added ${result.new}.`);
        }
      },
      error: (err) => {
        this.syncing.set(false);
        this.error.set(err?.message ?? 'Sync failed');
      }
    });
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
}
