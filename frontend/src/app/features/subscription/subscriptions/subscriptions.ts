import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { AuthService } from '../../../core/services/auth.service';
import { CareerProfileStore } from '../../../core/store/career-profile-store';
import { GeneralResponse, SubscriptionPlan } from '../../../core/models/payment.model';

@Component({
  selector: 'app-subscriptions',
  imports: [RouterLink],
  templateUrl: './subscriptions.html',
  styleUrl: './subscriptions.css',
})
export class Subscriptions implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private subscriptionService = inject(SubscriptionService);
  private authService = inject(AuthService);
  private store = inject(CareerProfileStore);

  plans = signal<SubscriptionPlan[]>([]);
  errorBanner = signal<string | null>(null);
  pendingBanner = signal<string | null>(null);
  loading = signal(true);
  navigating = signal<number | null>(null);

  activeSubscriptionId = computed(() => this.store.activeSubscription()?.subscriptionId ?? null);
  hasAnyActiveSub = computed(() => this.store.hasActiveSub());

  ngOnInit(): void {
    this.authService.syncRolesFromToken();
    this.store.refreshActiveSubscription();

    const payment = this.route.snapshot.queryParamMap.get('payment');
    if (payment === 'failed') {
      this.errorBanner.set('Payment failed. Please try again.');
    } else if (payment === 'pending') {
      this.pendingBanner.set('Payment is being processed. We\'ll update your subscription once confirmed.');
    }

    this.subscriptionService.getAll().subscribe({
      next: (res: GeneralResponse<SubscriptionPlan[]>) => {
        if (res.success && Array.isArray(res.data)) {
          this.plans.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  subscribe(planId: number): void {
    if (this.navigating() !== null) return;
    this.navigating.set(planId);
    this.router.navigate(['/payment', planId]);
  }

  isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  isCurrent(planId: number): boolean {
    return planId === this.activeSubscriptionId();
  }

  isNavigating(planId: number): boolean {
    return this.navigating() === planId;
  }
}
