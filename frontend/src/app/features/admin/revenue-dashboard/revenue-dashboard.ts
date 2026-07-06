import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { RevenueService } from '../../../core/services/revenue.service';
import { RevenueAnalyticsDto } from '../../../core/models/payment.model';

type DateRange = '30d' | '90d' | '6mo' | 'all';

@Component({
  selector: 'app-revenue-dashboard',
  imports: [DatePipe, DecimalPipe, FormsModule, BaseChartDirective],
  templateUrl: './revenue-dashboard.html',
  styleUrl: './revenue-dashboard.css',
})
export class RevenueDashboard implements OnInit {
  private revenueService = inject(RevenueService);

  analytics = signal<RevenueAnalyticsDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  range = signal<DateRange>('6mo');

  hasData = computed(() => this.analytics() !== null);
  summary = computed(() => this.analytics()?.summary);
  revenueByMonth = computed(() => this.analytics()?.revenueByMonth ?? []);
  planBreakdown = computed(() => this.analytics()?.subscriptionsByPlan ?? []);
  recentTransactions = computed(() => this.analytics()?.recentTransactions ?? []);

  rangeOptions: { value: DateRange; label: string }[] = [
    { value: '30d', label: 'Last 30 days' },
    { value: '90d', label: 'Last 90 days' },
    { value: '6mo', label: 'Last 6 months' },
    { value: 'all', label: 'All time' },
  ];

  lineChartData = computed<ChartData<'line'>>(() => {
    const points = this.revenueByMonth();
    return {
      labels: points.map(p => p.monthLabel),
      datasets: [{
        label: 'Revenue (EGP)',
        data: points.map(p => p.revenue),
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
      tooltip: {
        callbacks: {
          label: (ctx) => `${ctx.dataset.label}: EGP ${Number(ctx.parsed.y).toLocaleString()}`,
        },
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: { callback: (v) => `EGP ${Number(v) / 1000}k` },
      },
    },
  };

  doughnutChartData = computed<ChartData<'doughnut'>>(() => {
    const plans = this.planBreakdown();
    return {
      labels: plans.map(p => p.planName),
      datasets: [{
        data: plans.map(p => p.subscriberCount),
        backgroundColor: plans.map(p => p.color),
        borderWidth: 2,
        borderColor: '#fff',
      }],
    };
  });

  doughnutChartOptions: ChartOptions<'doughnut'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'right' },
      tooltip: {
        callbacks: {
          label: (ctx) => `${ctx.label}: ${ctx.parsed} subscribers`,
        },
      },
    },
    cutout: '65%',
  };

  ngOnInit(): void {
    this.loadAnalytics();
  }

  onRangeChange(newRange: DateRange): void {
    this.range.set(newRange);
    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.loading.set(true);
    this.error.set(null);

    const { from, to } = this.computeRange(this.range());

    this.revenueService.getAnalytics(from, to).subscribe({
      next: (data) => {
        this.analytics.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load analytics. Please try again.');
        this.loading.set(false);
      },
    });
  }

  churnClass(rate: number): string {
    if (rate >= 5) return 'churn-high';
    if (rate >= 2) return 'churn-medium';
    return 'churn-low';
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Paid': return 'status-paid';
      case 'Pending': return 'status-pending';
      case 'Failed': return 'status-failed';
      default: return '';
    }
  }

  private computeRange(range: DateRange): { from?: Date; to?: Date } {
    const now = new Date();
    switch (range) {
      case '30d': return { from: new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000), to: now };
      case '90d': return { from: new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000), to: now };
      case '6mo': return { from: new Date(now.getFullYear(), now.getMonth() - 6, now.getDate()), to: now };
      case 'all': return { to: now };
    }
  }
}
