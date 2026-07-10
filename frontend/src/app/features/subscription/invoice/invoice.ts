import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UserSubscriptionService } from '../../../core/services/user-subscription.service';
import { GeneralResponse, PaymentInvoiceDto } from '../../../core/models/payment.model';

@Component({
  selector: 'app-invoice',
  imports: [DatePipe, RouterLink],
  templateUrl: './invoice.html',
  styleUrl: './invoice.css',
})
export class Invoice implements OnInit {
  private route = inject(ActivatedRoute);
  private userSubscriptionService = inject(UserSubscriptionService);

  paymentId = 0;
  invoice = signal<PaymentInvoiceDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('paymentId');
    if (!id) {
      this.error.set('Invalid payment ID.');
      this.loading.set(false);
      return;
    }
    this.paymentId = Number(id);
    this.loadInvoice();
  }

  loadInvoice(): void {
    this.loading.set(true);
    this.error.set(null);

    this.userSubscriptionService.getPaymentInvoice(this.paymentId).subscribe({
      next: (res: GeneralResponse<PaymentInvoiceDto>) => {
        if (res.success && res.data) {
          this.invoice.set(res.data);
        } else {
          this.error.set('Invoice not found.');
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load invoice.');
        this.loading.set(false);
      },
    });
  }

  print(): void {
    window.print();
  }
}
