import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PaymentService } from '../../../core/services/payment.service';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { GeneralResponse, PaymentMethod, SubscriptionPlan } from '../../../core/models/payment.model';

@Component({
  selector: 'app-payment',
  imports: [],
  templateUrl: './payment.html',
  styleUrl: './payment.css',
})
export class Payment implements OnInit {
  private route = inject(ActivatedRoute);
  private paymentService = inject(PaymentService);
  private subscriptionService = inject(SubscriptionService);

  planId = '';
  plan = signal<SubscriptionPlan | null>(null);
  paymentMethods = signal<PaymentMethod[]>([]);
  userSubscriptionId = signal('');
  selectedMethodId = signal<number | null>(null);
  loading = signal(true);
  executing = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Invalid plan.');
      this.loading.set(false);
      return;
    }
    this.planId = id;
    this.loadPlanDetails();
  }

  loadPlanDetails(): void {
    this.subscriptionService.getById(this.planId).subscribe({
      next: (res: GeneralResponse<SubscriptionPlan>) => {
        if (res.success && res.data) {
          this.plan.set(res.data);
        }
      },
      error: () => {
      },
    });
    this.initPayment();
  }

  initPayment(): void {
    this.loading.set(true);
    this.error.set(null);

    this.paymentService.createPayment({ planId: this.planId }).subscribe({
      next: (res: GeneralResponse) => {
        if (!res.success) {
          this.error.set(typeof res.data === 'string' ? res.data : 'Failed to initialize payment.');
          this.loading.set(false);
          return;
        }
        const data = res as any;
        this.paymentMethods.set(Array.isArray(data.data) ? data.data : []);
        this.userSubscriptionId.set(data.userSubscriptionId || '');
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not connect to payment service.');
        this.loading.set(false);
      },
    });
  }

  selectMethod(methodId: number): void {
    if (this.executing()) return;
    this.selectedMethodId.set(methodId);
    this.executing.set(true);
    this.error.set(null);

    this.paymentService.executeInvoice(String(methodId), this.userSubscriptionId()).subscribe({
      next: (res: any) => {
        const redirectUrl = res?.data?.Payment_Data?.RedirectTo;
        if (redirectUrl) {
          window.location.href = redirectUrl;
        } else {
          this.error.set('No redirect URL received from payment gateway.');
          this.executing.set(false);
        }
      },
      error: () => {
        this.error.set('Payment execution failed. Please try again.');
        this.executing.set(false);
        this.selectedMethodId.set(null);
      },
    });
  }
}
