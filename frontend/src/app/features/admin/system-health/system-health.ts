import { Component, inject, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { HealthCheckDto } from '../../../core/models/admin.model';

@Component({
  selector: 'app-system-health',
  imports: [DatePipe],
  templateUrl: './system-health.html',
  styleUrl: './system-health.css',
})
export class SystemHealth implements OnInit, OnDestroy {
  private adminService = inject(AdminService);

  health = signal<HealthCheckDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  autoRefresh = signal(true);
  lastUpdated = signal<Date | null>(null);

  private refreshInterval: ReturnType<typeof setInterval> | null = null;

  dbOk = computed(() => this.health()?.db.status === 'healthy');
  llmOk = computed(() => this.health()?.llm.status === 'healthy');
  jobOk = computed(() => this.health()?.jobProvider.status === 'healthy');
  storageOk = computed(() => this.health()?.storage.status === 'healthy' || this.health()?.storage.status === 'warning');
  allHealthy = computed(() => this.dbOk() && this.llmOk() && this.jobOk() && this.storageOk());

  ngOnInit(): void {
    this.loadHealth();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  loadHealth(): void {
    this.loading.set(true);
    this.error.set(null);

    this.adminService.getHealth().subscribe({
      next: h => {
        this.health.set(h);
        this.lastUpdated.set(new Date());
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load system health');
        this.loading.set(false);
      },
    });
  }

  startAutoRefresh(): void {
    this.stopAutoRefresh();
    this.refreshInterval = setInterval(() => {
      if (this.autoRefresh()) {
        this.loadHealth();
      }
    }, 30000);
  }

  stopAutoRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
      this.refreshInterval = null;
    }
  }

  toggleAutoRefresh(): void {
    this.autoRefresh.update(v => !v);
    if (this.autoRefresh()) {
      this.startAutoRefresh();
    } else {
      this.stopAutoRefresh();
    }
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return (bytes / Math.pow(1024, i)).toFixed(1) + ' ' + units[i];
  }

  formatTimestamp(iso?: string): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleString();
  }
}
