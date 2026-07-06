import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AdminSubscriptionService } from '../../../core/services/admin-subscription.service';
import { SubscriberDetailDto, AuditLogDto, ExtendSubscriptionRequest } from '../../../core/models/payment.model';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';
import { ExtendModal } from '../subscriber-actions/extend-modal/extend-modal';

@Component({
  selector: 'app-subscriber-detail',
  imports: [DatePipe, DecimalPipe, ConfirmModal, ExtendModal],
  templateUrl: './subscriber-detail.html',
  styleUrl: './subscriber-detail.css',
})
export class SubscriberDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminSubscriptionService = inject(AdminSubscriptionService);

  detail = signal<SubscriberDetailDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  actionLoading = signal<string | null>(null);

  user = computed(() => this.detail()?.user);
  subscription = computed(() => this.detail()?.subscription);
  recentPayments = computed(() => this.detail()?.recentPayments ?? []);
  auditLogEntries = computed(() => this.detail()?.auditLogEntries ?? []);
  recentSessions = computed(() => this.detail()?.recentSessions ?? []);
  cvs = computed(() => this.detail()?.cvs ?? []);
  roadmaps = computed(() => this.detail()?.roadmaps ?? []);

  daysRemaining = computed(() => {
    const sub = this.subscription();
    if (!sub?.endDate) return null;
    const diff = new Date(sub.endDate).getTime() - Date.now();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  });

  isActive = computed(() => {
    const sub = this.subscription();
    return !!sub?.isActive && (this.daysRemaining() ?? 0) > 0;
  });

  hasPendingPayments = computed(() =>
    this.recentPayments().some(p => p.status === 'Pending')
  );

  showExtendModal = signal(false);
  confirmAction = signal<{ type: string; title: string; message: string; extraData?: number } | null>(null);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set('Invalid subscription ID');
      return;
    }
    this.loadDetail(id);
  }

  loadDetail(id: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.adminSubscriptionService.getDetail(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.detail.set(res.data);
        } else {
          this.error.set('Subscriber not found.');
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load subscriber details.');
        this.loading.set(false);
      },
    });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Paid': return 'status-paid';
      case 'Pending': return 'status-pending';
      case 'Failed': return 'status-failed';
      default: return '';
    }
  }

  goBack(): void {
    this.router.navigate(['/admin/subscribers']);
  }

  requestActivate(): void {
    const sub = this.subscription();
    if (!sub) return;
    this.confirmAction.set({
      type: 'activate',
      title: 'Activate Subscription?',
      message: `Activate ${this.user()?.fullName || 'this user'}'s subscription?`,
    });
  }

  requestCancel(): void {
    const sub = this.subscription();
    if (!sub) return;
    this.confirmAction.set({
      type: 'cancel',
      title: 'Cancel Subscription?',
      message: `Cancel ${this.user()?.fullName || 'this user'}'s subscription? They will keep access until the current period ends.`,
    });
  }

  requestCancelImmediate(): void {
    const sub = this.subscription();
    if (!sub) return;
    this.confirmAction.set({
      type: 'cancel-immediate',
      title: 'Cancel Immediately?',
      message: `Immediately cancel ${this.user()?.fullName || 'this user'}'s subscription? Access will end right away.`,
    });
  }

  requestMarkPaid(): void {
    const payments = this.recentPayments();
    const pending = payments.filter(p => p.status === 'Pending');
    if (pending.length === 0) return;
    this.confirmAction.set({
      type: 'mark-paid',
      title: 'Mark Payment as Paid?',
      message: `Mark the pending payment of EGP ${pending[0].amount} as paid for ${this.user()?.fullName || 'this user'}?`,
    });
  }

  requestRefund(paymentId: number): void {
    this.confirmAction.set({
      type: 'refund',
      title: 'Refund Payment?',
      message: `Refund this payment? A negative payment record will be created for tracking. This does NOT issue a real refund to the user.`,
      extraData: paymentId,
    });
  }

  closeConfirm(): void {
    this.confirmAction.set(null);
  }

  handleConfirm(): void {
    const action = this.confirmAction();
    if (!action) return;
    const id = this.subscription()?.id;
    if (!id) return;

    this.actionLoading.set(action.type);

    switch (action.type) {
      case 'activate':
        this.adminSubscriptionService.activate(id).subscribe({ next: () => this.onActionSuccess(), error: (e) => this.onActionError(e) });
        break;
      case 'cancel':
        this.adminSubscriptionService.cancel(id, undefined, false).subscribe({ next: () => this.onActionSuccess(), error: (e) => this.onActionError(e) });
        break;
      case 'cancel-immediate':
        this.adminSubscriptionService.cancel(id, 'Immediate cancel by admin', true).subscribe({ next: () => this.onActionSuccess(), error: (e) => this.onActionError(e) });
        break;
      case 'mark-paid': {
        const paymentId = this.recentPayments().filter(p => p.status === 'Pending')[0]?.paymentId;
        if (!paymentId) { this.actionLoading.set(null); this.confirmAction.set(null); return; }
        this.adminSubscriptionService.markPaymentPaid(paymentId).subscribe({ next: () => this.onActionSuccess(), error: (e) => this.onActionError(e) });
        break;
      }
      case 'refund': {
        const paymentId = action.extraData as number;
        this.adminSubscriptionService.refundPayment(paymentId).subscribe({ next: () => this.onActionSuccess(), error: (e) => this.onActionError(e) });
        break;
      }
      default:
        this.actionLoading.set(null);
        this.confirmAction.set(null);
    }
  }

  openExtendModal(): void {
    this.showExtendModal.set(true);
  }

  closeExtendModal(): void {
    this.showExtendModal.set(false);
  }

  handleExtend(request: ExtendSubscriptionRequest): void {
    const id = this.subscription()?.id;
    if (!id) return;
    this.actionLoading.set('extend');
    this.adminSubscriptionService.extend(id, request).subscribe({
      next: () => {
        this.actionLoading.set(null);
        this.showExtendModal.set(false);
        this.loadDetail(id);
      },
      error: (e) => {
        this.actionLoading.set(null);
        this.error.set('Failed to extend subscription.');
      },
    });
  }

  private onActionSuccess(): void {
    this.actionLoading.set(null);
    this.confirmAction.set(null);
    const id = this.subscription()?.id;
    if (id) this.loadDetail(id);
  }

  private onActionError(err: any): void {
    this.actionLoading.set(null);
    this.confirmAction.set(null);
    this.error.set('Action failed. Please try again.');
  }
}
