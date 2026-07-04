import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PaymentService } from '../../../core/services/payment.service';

@Component({
  selector: 'app-payment',
  imports: [],
  templateUrl: './payment.html',
  styleUrl: './payment.css',
})
export class Payment implements OnInit {
  private route = inject(ActivatedRoute);
  private paymentService = inject(PaymentService);

  ngOnInit(): void {
    const planId = this.route.snapshot.paramMap.get('id');
    if (planId) {
      this.paymentService.createPayment({ planId, userId: '' }).subscribe();
    }
  }
}
