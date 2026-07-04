import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { SubscriptionServices } from '../../../core/services/subscription.services';

import {
  createSubscriptionResponse
} from '../../../core/models/getallsubscriptionresponse.models';

@Component({
  selector: 'app-update-subscription',
  standalone: true,
  imports: [CommonModule,FormsModule],
  templateUrl: './update-subscription.html',
  styleUrl: './update-subscription.css'
})
export class UpdateSubscriptionComponent implements OnInit {

  subscriptionId!: string;

  plan: createSubscriptionResponse = {
    name: '',
    price: 0
  };

  loading = true;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private subscriptionService: SubscriptionServices
  ) {}

  ngOnInit(): void {

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      alert("Invalid subscription id.");
      this.router.navigate(['/subscriptions']);
      return;
    }
    this.subscriptionId = id;
    this.loadSubscription();

  }

  loadSubscription(): void {
    this.subscriptionService
      .getbyid(this.subscriptionId)
      .subscribe({
        next: (response:createSubscriptionResponse) => {
          this.plan = response;
          this.loading = false;
        },

        error: (err:any) => {
          console.error(err);
          alert("Unable to load subscription.");
          this.router.navigate(['/subscriptions']);

        }

      });

  }

  update(): void {
  this.subscriptionService.updateSubscription(this.subscriptionId,this.plan)
      .subscribe({
        next: () => {
          alert("Subscription updated successfully.");
          this.router.navigate(['/subscriptions']);
        },

        error: (err:any) => {
          console.error(err);
          alert("Update failed.");

        }

      });

  }
  cancel(): void {
    this.router.navigate(['/subscriptions']);

  }

}