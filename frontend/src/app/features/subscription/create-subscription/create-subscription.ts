import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { CreateSubscriptionRequest } from '../../../core/models/payment.model';

@Component({
  selector: 'app-create-subscription',
  imports: [FormsModule],
  templateUrl: './create-subscription.html',
  styleUrl: './create-subscription.css',
})
export class CreateSubscription {
  private router = inject(Router);
  private subscriptionService = inject(SubscriptionService);

  plan: CreateSubscriptionRequest = { name: '', price: 0 };

  create(): void {
    this.subscriptionService.create(this.plan).subscribe({
      next: () => this.router.navigate(['/subscriptions']),
      error: (err) => console.error('error', err),
    });
  }

  cancel(): void {
    this.router.navigate(['/subscriptions']);
  }
}
