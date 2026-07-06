import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { UserSubscriptionService } from '../../../core/services/user-subscription.service';
import { GeneralResponse, PaymentInvoiceDto, PagedPaymentHistoryDto } from '../../../core/models/payment.model';

@Component({
  selector: 'app-payment-history',
  imports: [DatePipe, RouterLink],
  templateUrl: './payment-history.html',
  styleUrl: './payment-history.css',
})
export class PaymentHistory implements OnInit {
  private userSubscriptionService = inject(UserSubscriptionService);

  payments = signal<PaymentInvoiceDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  hasNextPage = signal(false);

  canGoPrev = computed(() => this.page() > 1);
  canGoNext = computed(() => this.hasNextPage());

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.loading.set(true);
    this.error.set(null);

    this.userSubscriptionService.getPaymentHistory(this.page(), this.pageSize()).subscribe({
      next: (res: GeneralResponse<PagedPaymentHistoryDto>) => {
        if (res.success && res.data) {
          this.payments.set(res.data.items);
          this.totalCount.set(res.data.totalCount);
          this.hasNextPage.set(res.data.hasNextPage);
        } else {
          this.payments.set([]);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load payment history.');
        this.loading.set(false);
      },
    });
  }

  nextPage(): void {
    if (!this.canGoNext()) return;
    this.page.update(p => p + 1);
    this.loadHistory();
  }

  prevPage(): void {
    if (!this.canGoPrev()) return;
    this.page.update(p => p - 1);
    this.loadHistory();
  }
}
