import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { AdminService } from '../../../core/services/admin.service';
import { ReportsDto } from '../../../core/models/admin.model';

@Component({
  selector: 'app-reports',
  imports: [BaseChartDirective],
  templateUrl: './reports.html',
  styleUrl: './reports.css',
})
export class Reports implements OnInit {
  private adminService = inject(AdminService);

  reports = signal<ReportsDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  exporting = signal(false);
  exportError = signal<string | null>(null);

  hasData = computed(() => this.reports() !== null);
  usersOverTime = computed(() => this.reports()?.usersOverTime ?? []);
  interviewsPerDay = computed(() => this.reports()?.interviewsPerDay ?? []);
  topRequestedRoles = computed(() => this.reports()?.topRequestedRoles ?? []);
  popularSkills = computed(() => this.reports()?.popularSkills ?? []);

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.loading.set(true);
    this.error.set(null);

    this.adminService.getReports().subscribe({
      next: data => {
        this.reports.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load reports. Please try again.');
        this.loading.set(false);
      },
    });
  }

  lineChartData = computed<ChartData<'line'>>(() => {
    const points = this.usersOverTime();
    return {
      labels: points.map(p => p.month),
      datasets: [{
        label: 'New Users',
        data: points.map(p => p.count),
        fill: 'origin',
        tension: 0.35,
        borderColor: '#2563EB',
        backgroundColor: 'rgba(37, 99, 235, 0.12)',
        pointBackgroundColor: '#2563EB',
        pointBorderColor: '#fff',
        pointRadius: 4,
        pointHoverRadius: 6,
      }],
    };
  });

  lineChartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: { display: true, position: 'bottom' },
    },
    scales: {
      y: { beginAtZero: true, ticks: { stepSize: 1 } },
    },
  };

  barChartData = computed<ChartData<'bar'>>(() => {
    const points = this.interviewsPerDay();
    return {
      labels: points.map(p => p.date),
      datasets: [{
        label: 'Interviews',
        data: points.map(p => p.count),
        backgroundColor: '#10B981',
        borderRadius: 4,
        borderSkipped: false,
      }],
    };
  });

  barChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: { display: true, position: 'bottom' },
    },
    scales: {
      y: { beginAtZero: true, ticks: { stepSize: 1 } },
    },
  };

  exportCsv(type: string): void {
    this.exporting.set(true);
    this.exportError.set(null);

    this.adminService.exportCsv(type).subscribe({
      next: blob => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${type}-${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.exporting.set(false);
      },
      error: () => {
        this.exportError.set(`Failed to export ${type} CSV. Please try again.`);
        this.exporting.set(false);
      },
    });
  }

  exportOpen = signal(false);

  toggleExport(): void {
    this.exportOpen.update(v => !v);
  }
}
