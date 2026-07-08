import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { AdminService } from '../../../core/services/admin.service';
import { DashboardStatistics } from '../../../core/models/admin.model';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

interface WidgetDef {
  key: string;
  label: string;
  icon: string;
  color: string;
  getValue: (s: DashboardStatistics) => string;
}

interface WidgetConfig {
  order: string[];
  hidden: string[];
}

const WIDGETS_KEY = 'adminDashboardWidgets';

const WIDGET_DEFS: WidgetDef[] = [
  { key: 'users', label: 'Total Users', icon: 'people', color: '#2563EB', getValue: s => s.users.toString() },
  { key: 'admins', label: 'Admins', icon: 'admin_panel_settings', color: '#7C3AED', getValue: s => s.admins.toString() },
  { key: 'cvs', label: 'CVs Uploaded', icon: 'description', color: '#059669', getValue: s => s.cVs.toString() },
  { key: 'interviews', label: 'Interviews', icon: 'record_voice_over', color: '#D97706', getValue: s => s.interviews.toString() },
  { key: 'revenue', label: 'Total Revenue', icon: 'payments', color: '#2563EB', getValue: s => 'EGP ' + s.totalRevenue.toLocaleString() },
  { key: 'subscriptions', label: 'Active Subscriptions', icon: 'verified', color: '#16A34A', getValue: s => s.activeSubscriptions.toString() },
];

const DEFAULT_ORDER = WIDGET_DEFS.map(w => w.key);

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, StatCard, ConfirmModal, DragDropModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
})
export class AdminDashboard implements OnInit {
  private adminService = inject(AdminService);
  protected widgetDefs = WIDGET_DEFS;

  loading = signal(false);
  error = signal<string | null>(null);
  stats = signal<DashboardStatistics | null>(null);
  cacheClearing = signal(false);
  showClearCacheModal = signal(false);
  cacheClearResult = signal<string | null>(null);

  widgetOrder = signal<string[]>([...DEFAULT_ORDER]);
  hiddenWidgets = signal<string[]>([]);
  showCustomizer = signal(false);

  displayedKeys = computed(() =>
    this.widgetOrder().filter(k => !this.hiddenWidgets().includes(k))
  );

  quickLinks = [
    { path: '/admin/users', label: 'Users', icon: 'people', desc: 'View and manage all users' },
    { path: '/admin/jobs', label: 'Jobs', icon: 'work', desc: 'Manage job listings and sync' },
    { path: '/admin/plans', label: 'Plans', icon: 'card_membership', desc: 'Manage subscription plans' },
    { path: '/admin/subscribers', label: 'Subscribers', icon: 'group', desc: 'View and manage subscribers' },
    { path: '/admin/revenue', label: 'Revenue', icon: 'insights', desc: 'Revenue analytics and charts' },
    { path: '/admin/audit-log', label: 'Audit Log', icon: 'history', desc: 'View admin activity and changes' },
    { path: '/admin/reports', label: 'Reports', icon: 'bar_chart', desc: 'Platform growth reports and data export' },
    { path: '/admin/health', label: 'System Health', icon: 'monitor_heart', desc: 'Monitor services and system status' },
    { path: '/admin/chat', label: 'Chat Sessions', icon: 'chat', desc: 'Browse user chat sessions and transcripts' },
    { path: '/admin/broadcast', label: 'Broadcast', icon: 'campaign', desc: 'Send notifications to users' },
  ];

  ngOnInit(): void {
    const saved = localStorage.getItem(WIDGETS_KEY);
    if (saved) {
      try {
        const cfg: WidgetConfig = JSON.parse(saved);
        const validKeys = new Set(DEFAULT_ORDER);
        this.widgetOrder.set(cfg.order.filter(k => validKeys.has(k)));
        this.hiddenWidgets.set(cfg.hidden.filter(k => validKeys.has(k)));
      } catch {
        /* ignore corrupt config */
      }
    }
    this.loadAll();
  }

  loadAll(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService.getStatistics().subscribe({
      next: s => { this.stats.set(s); this.loading.set(false); },
      error: () => { this.error.set('Failed to load statistics'); this.loading.set(false); },
    });
  }

  onDrop(event: CdkDragDrop<string[]>): void {
    const order = [...this.widgetOrder()];
    moveItemInArray(order, event.previousIndex, event.currentIndex);
    this.widgetOrder.set(order);
    this.saveConfig();
  }

  toggleWidget(key: string): void {
    this.hiddenWidgets.update(h =>
      h.includes(key) ? h.filter(k => k !== key) : [...h, key]
    );
    this.saveConfig();
  }

  toggleCustomizer(): void {
    this.showCustomizer.update(v => !v);
  }

  closeCustomizer(): void {
    this.showCustomizer.set(false);
  }

  resetToDefault(): void {
    this.widgetOrder.set([...DEFAULT_ORDER]);
    this.hiddenWidgets.set([]);
    this.saveConfig();
  }

  widgetDef(key: string): WidgetDef {
    return WIDGET_DEFS.find(w => w.key === key)!;
  }

  private saveConfig(): void {
    localStorage.setItem(WIDGETS_KEY, JSON.stringify({
      order: this.widgetOrder(),
      hidden: this.hiddenWidgets(),
    }));
  }

  requestClearCache(): void {
    this.showClearCacheModal.set(true);
  }

  confirmClearCache(): void {
    this.showClearCacheModal.set(false);
    this.cacheClearing.set(true);
    this.cacheClearResult.set(null);
    this.adminService.clearCache().subscribe({
      next: () => { this.cacheClearing.set(false); this.cacheClearResult.set('AI cache cleared successfully.'); },
      error: () => { this.cacheClearing.set(false); this.cacheClearResult.set('Failed to clear AI cache. Please try again.'); },
    });
  }

  cancelClearCache(): void {
    this.showClearCacheModal.set(false);
  }
}
