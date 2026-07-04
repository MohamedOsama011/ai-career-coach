import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { GeneralResponse } from '../../../core/models/payment.model';

@Component({
  selector: 'app-subscriptions',
  imports: [],
  templateUrl: './subscriptions.html',
  styleUrl: './subscriptions.css',
})
export class Subscriptions implements OnInit {
  private router = inject(Router);
  private subscriptionService = inject(SubscriptionService);

  plans: any[] = [];

  ngOnInit(): void {
    this.subscriptionService.getAll().subscribe({
      next: (res: GeneralResponse) => {
        if (res.success && Array.isArray(res.data)) {
          this.plans = res.data;
        }
      },
    });
  }

  navigateToCreate(): void {
    this.router.navigate(['/create-subscription']);
  }
}
