import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { UserSubscriptionService } from '../../../core/services/user-subscription.service';
import { CareerProfileStore } from '../../../core/store/career-profile-store';
import { SubscriptionGateService, GateFeature } from '../../../core/services/subscription-gate.service';
import { GeneralResponse, UserSubscriptionDto } from '../../../core/models/payment.model';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-my-subscriptions',
  imports: [RouterLink, DatePipe, ConfirmModal],
  templateUrl: './my-subscriptions.html',
  styleUrl: './my-subscriptions.css',
})
export class MySubscriptions implements OnInit {
  private route = inject(ActivatedRoute);
  private userSubscriptionService = inject(UserSubscriptionService);
  protected store = inject(CareerProfileStore);
  private gateService = inject(SubscriptionGateService);

  subscriptions = signal<UserSubscriptionDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  successBanner = signal<string | null>(null);

  pendingCancelId = signal<number | null>(null);
  cancelling = signal(false);

  pendingCancelSub = computed<UserSubscriptionDto | null>(() => {
    const id = this.pendingCancelId();
    if (id === null) return null;
    return this.subscriptions().find(s => s.id === id) ?? null;
  });

  daysRemaining = computed(() => {
    const active = this.subscriptions().find(s => s.isActive);
    if (!active || !active.endDate) return null;
    const now = Date.now();
    const end = new Date(active.endDate).getTime();
    const diff = Math.ceil((end - now) / (1000 * 60 * 60 * 24));
    return diff > 0 ? diff : 0;
  });

  isExpiringSoon = computed(() => {
    const d = this.daysRemaining();
    return d !== null && d <= 7;
  });

  hasActivePaidSub = computed(() => {
    const active = this.subscriptions().find(s => s.isActive);
    return !!active;
  });

  usageStats = computed(() => this.store.gateStatus());

  ngOnInit(): void {
    const payment = this.route.snapshot.queryParamMap.get('payment');
    if (payment === 'success') {
      this.successBanner.set('Payment successful! Your subscription is now active.');
      setTimeout(() => this.successBanner.set(null), 5000);
    }
    this.loadSubscriptions();
    this.store.refreshGateStatus();
    this.store.refreshPaymentHistory();
  }

  private loadSubscriptions(): void {
    this.loading.set(true);
    this.error.set(null);

    this.userSubscriptionService.getMy().subscribe({
      next: (res: GeneralResponse<UserSubscriptionDto[]>) => {
        this.subscriptions.set(Array.isArray(res.data) ? res.data : []);
        this.loading.set(false);
        this.store.refreshActiveSubscription();
      },
      error: () => {
        this.error.set('Failed to load subscriptions.');
        this.loading.set(false);
      },
    });
  }

  requestCancel(id: number): void {
    this.pendingCancelId.set(id);
  }

  cancelCancel(): void {
    this.pendingCancelId.set(null);
  }

  confirmCancel(): void {
    const id = this.pendingCancelId();
    if (id === null) return;

    this.cancelling.set(true);
    const snapshot = this.subscriptions();
    this.subscriptions.update(list =>
      list.map(s => (s.id === id
        ? { ...s, isActive: false, status: 'Cancelled' as const }
        : s))
    );

    this.userSubscriptionService.cancelSubscription(id).subscribe({
      next: () => {
        this.cancelling.set(false);
        this.pendingCancelId.set(null);
        this.store.refreshActiveSubscription();
        this.store.refreshPaymentHistory();
      },
      error: () => {
        this.cancelling.set(false);
        this.pendingCancelId.set(null);
        this.subscriptions.set(snapshot);
        this.error.set('Failed to cancel subscription.');
      },
    });
  }

  getCancelMessage(): string {
    const sub = this.pendingCancelSub();
    if (!sub) return 'You will lose access at the end of the current period.';
    if (sub.endDate) {
      const end = new Date(sub.endDate);
      return `Are you sure you want to cancel "${sub.subscription?.name ?? 'this subscription'}"? You will keep access until ${end.toLocaleDateString()}.`;
    }
    return `Are you sure you want to cancel "${sub.subscription?.name ?? 'this subscription'}"? You will lose access at the end of the current period.`;
  }

  used(feature: GateFeature): number {
    const stats = this.usageStats();
    if (!stats) return 0;
    const f = (stats.features as Record<string, { used: number; limit: number; allowed: boolean }>)[feature];
    return f?.used ?? 0;
  }

  limit(feature: GateFeature): number {
    const stats = this.usageStats();
    if (!stats) return 0;
    const f = (stats.features as Record<string, { used: number; limit: number; allowed: boolean }>)[feature];
    return f?.limit ?? 0;
  }
}
