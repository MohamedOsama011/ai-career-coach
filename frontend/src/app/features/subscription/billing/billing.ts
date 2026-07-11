import { Component, inject, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { UserSubscriptionService } from '../../../core/services/user-subscription.service';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { CareerProfileStore } from '../../../core/store/career-profile-store';
import { GateFeature } from '../../../core/services/subscription-gate.service';
import { GeneralResponse, SubscriptionPlan, UserSubscriptionDto, PlanLimits } from '../../../core/models/payment.model';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-billing',
  imports: [RouterLink, DatePipe, ConfirmModal],
  templateUrl: './billing.html',
  styleUrl: './billing.css',
})
export class Billing implements OnInit, OnDestroy {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private subscriptionService = inject(SubscriptionService);
  private userSubscriptionService = inject(UserSubscriptionService);
  private paymentService = inject(PaymentService);
  private authService = inject(AuthService);
  protected store = inject(CareerProfileStore);

  plans = signal<SubscriptionPlan[]>([]);
  subscriptions = signal<UserSubscriptionDto[]>([]);
  loadingPlans = signal(true);
  loadingSubscriptions = signal(true);
  errorBanner = signal<string | null>(null);
  pendingBanner = signal<string | null>(null);
  successBanner = signal<string | null>(null);
  navigating = signal<number | null>(null);
  pendingCancelId = signal<number | null>(null);
  cancelling = signal(false);

  activeSubscriptionId = computed(() => this.store.activeSubscription()?.subscriptionId ?? null);
  hasAnyActiveSub = computed(() => this.store.hasActiveSub());

  pendingCancelSub = computed<UserSubscriptionDto | null>(() => {
    const id = this.pendingCancelId();
    if (id === null) return null;
    return this.subscriptions().find(s => s.id === id) ?? null;
  });

  daysRemaining = computed<number | null>(() => {
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

  usageStats = computed(() => this.store.gateStatus());

  loading = computed(() => this.loadingPlans() || this.loadingSubscriptions());

  private pollTimer: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.authService.syncRolesFromToken();
    this.store.refreshActiveSubscription();
    this.store.refreshPaymentHistory();
    this.store.refreshGateStatus();

    const payment = this.route.snapshot.queryParamMap.get('payment');
    if (payment === 'success') {
      this.successBanner.set('Payment successful! Activating your subscription...');
      this.confirmPaymentOnRedirect();
    } else if (payment === 'failed') {
      this.errorBanner.set('Payment failed. Please try again.');
    } else if (payment === 'pending') {
      this.pendingBanner.set('Payment is being processed. We\'ll update your subscription once confirmed.');
    }

    this.loadData();
  }

  private confirmPaymentOnRedirect(): void {
    let attempts = 0;
    const maxAttempts = 5;
    const tryConfirm = (): void => {
      attempts++;
      this.paymentService.confirmPayment().subscribe({
        next: (res) => {
          if (res.success) {
            this.successBanner.set('Payment successful! Your subscription is now active.');
            setTimeout(() => this.successBanner.set(null), 5000);
            this.refreshSubscriptions();
            this.store.refreshGateStatus();
          } else if (attempts < maxAttempts) {
            setTimeout(tryConfirm, 2000);
          } else {
            this.startPolling();
          }
        },
        error: () => {
          if (attempts < maxAttempts) {
            setTimeout(tryConfirm, 2000);
          } else {
            this.startPolling();
          }
        },
      });
    };
    tryConfirm();
  }

  private startPolling(): void {
    let attempts = 0;
    const maxAttempts = 15;
    this.pollTimer = setInterval(() => {
      attempts++;
      this.userSubscriptionService.getMy().subscribe({
        next: (res) => {
          const subs = Array.isArray(res.data) ? res.data : [];
          this.subscriptions.set(subs);
          this.store.refreshActiveSubscription();
          const hasActive = subs.some(s => s.isActive && s.status === 'Active');
          if (hasActive) {
            this.successBanner.set('Payment successful! Your subscription is now active.');
            setTimeout(() => this.successBanner.set(null), 5000);
            this.clearPollTimer();
            this.store.refreshGateStatus();
          } else if (attempts >= maxAttempts) {
            this.clearPollTimer();
            this.pendingBanner.set('Payment is being processed. Refresh the page to check your subscription status.');
          }
        },
        error: () => {
          if (attempts >= maxAttempts) this.clearPollTimer();
        },
      });
    }, 2000);
  }

  private clearPollTimer(): void {
    if (this.pollTimer !== null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  private refreshSubscriptions(): void {
    this.userSubscriptionService.getMy().subscribe({
      next: (res) => {
        this.subscriptions.set(Array.isArray(res.data) ? res.data : []);
        this.store.refreshActiveSubscription();
      },
    });
  }

  ngOnDestroy(): void {
    this.clearPollTimer();
  }

  private loadData(): void {
    this.loadingPlans.set(true);
    this.loadingSubscriptions.set(true);

    this.subscriptionService.getAll().subscribe({
      next: (res: GeneralResponse<SubscriptionPlan[]>) => {
        if (res.success && Array.isArray(res.data)) {
          const order = ['Basic', 'Pro', 'Premium'];
          const sorted = [...res.data].sort(
            (a, b) => order.indexOf(a.name) - order.indexOf(b.name)
          );
          this.plans.set(sorted);
        }
        this.loadingPlans.set(false);
      },
      error: () => {
        this.loadingPlans.set(false);
      },
    });

    this.userSubscriptionService.getMy().subscribe({
      next: (res: GeneralResponse<UserSubscriptionDto[]>) => {
        this.subscriptions.set(Array.isArray(res.data) ? res.data : []);
        this.loadingSubscriptions.set(false);
        this.store.refreshActiveSubscription();
      },
      error: () => {
        this.loadingSubscriptions.set(false);
      },
    });
  }

  subscribe(planId: number): void {
    if (this.navigating() !== null) return;
    if (this.isCurrent(planId)) return;
    this.navigating.set(planId);
    this.router.navigate(['/payment', planId]);
  }

  isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  isCurrent(planId: number): boolean {
    if (planId === this.activeSubscriptionId()) return true;
    if (!this.hasAnyActiveSub()) {
      const plan = this.plans().find(p => p.id === planId);
      if (plan?.price === 0) return true;
    }
    return false;
  }

  isNavigating(planId: number): boolean {
    return this.navigating() === planId;
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
        this.errorBanner.set('Failed to cancel subscription.');
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

  planFeatures(plan: SubscriptionPlan): { text: string; included: boolean }[] {
    const limits = this.parseLimits(plan.limitsJson);
    const base = [
      {
        text: limits.interviewSessions === -1 ? 'Unlimited Interviews'
              : `${limits.interviewSessions} Interview${limits.interviewSessions !== 1 ? 's' : ''}`,
        included: limits.interviewSessions !== 0,
      },
      {
        text: limits.roadmapGenerations === -1 ? 'Unlimited Roadmaps'
              : `${limits.roadmapGenerations} Roadmap${limits.roadmapGenerations !== 1 ? 's' : ''}`,
        included: limits.roadmapGenerations !== 0,
      },
      {
        text: limits.jobRecommendations === -1 ? 'Unlimited Job Matches'
              : `${limits.jobRecommendations} Job${limits.jobRecommendations !== 1 ? 's' : ''}`,
        included: limits.jobRecommendations !== 0,
      },
      {
        text: limits.roadmapRescan ? 'Skills Rescan' : 'No Rescan',
        included: limits.roadmapRescan,
      },
    ];
    return base;
  }

  planMeta(plan: SubscriptionPlan): { tagline: string; supportLabel: string; accentClass: string } {
    if (plan.price === 0) {
      return {
        tagline: 'Essential tools to start your journey',
        supportLabel: 'Community Support',
        accentClass: 'accent-basic',
      };
    }
    if (plan.price >= 999) {
      return {
        tagline: 'The complete career transformation package',
        supportLabel: 'Priority Support',
        accentClass: 'accent-premium',
      };
    }
    return {
      tagline: 'Best value for serious job seekers',
      supportLabel: 'Email Support',
      accentClass: 'accent-pro',
    };
  }

  private parseLimits(json: string | null | undefined): PlanLimits {
    if (!json) return { interviewSessions: 1, roadmapGenerations: 1, jobRecommendations: 3, roadmapRescan: false };
    try {
      const raw = JSON.parse(json);
      return {
        interviewSessions: raw.interviewSessions ?? raw.InterviewSessions ?? 1,
        roadmapGenerations: raw.roadmapGenerations ?? raw.RoadmapGenerations ?? 1,
        jobRecommendations: raw.jobRecommendations ?? raw.JobRecommendations ?? 3,
        roadmapRescan: raw.roadmapRescan ?? raw.RoadmapRescan ?? false,
      };
    } catch {
      return { interviewSessions: 1, roadmapGenerations: 1, jobRecommendations: 3, roadmapRescan: false };
    }
  }

  isPopular(plan: SubscriptionPlan): boolean {
    return plan.price === 399;
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
