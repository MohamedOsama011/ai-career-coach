import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { SubscriptionServices } from '../../core/services/subscription.services';
import {SubscriptionPlan,getallsubscriptionresponse} from '../../core/models/getallsubscriptionresponse.models';

@Component({
  selector: 'app-subscriptions',
  standalone: true,
  imports: [CommonModule,RouterLink],
})
export class Subscriptions implements OnInit {

  plans: SubscriptionPlan[]|string = [];

  constructor(
    private subscriptionService: SubscriptionServices,
    private router: Router
  ) {}

  ngOnInit(): void {this.loadPlans();}

  loadPlans(): void {
    this.subscriptionService.getSubscriptions().subscribe({
      next: (response: getallsubscriptionresponse) => {
        this.plans = response.data;
      },
      error: (err:any) => { console.error(err);
alert("Failed to load subscriptions.");}
});
}

// planisstring(s:object):boolean{
//   typeof(s)==String?return true :return false
// if (typeof(s)==String)
//   return true
// return false
// }

  deletePlan(id: number): void {
const confirmDelete = confirm("Are you sure you want to delete this subscription?");
    if (!confirmDelete)
      return;
    this.subscriptionService.deleteSubscription(id.toString())
      .subscribe({next: () => {
        alert("Subscription deleted successfully.");
        this.loadPlans();},
        error:(err:any) => {console.error(err);alert("Delete failed.") }

      });

  }

  goToCreate(): void {
    this.router.navigate(['/subscriptions/create']);
  }
  goToView(id: number): void {
    this.router.navigate(['/subscriptions/view', id]);
  }

  goToUpdate(id: number): void {
    this.router.navigate(['/subscriptions/update', id]);
  }

}