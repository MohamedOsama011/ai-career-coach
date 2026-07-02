import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

import { SubscriptionServices } from '../../core/services/subscription.services';
import {createSubscriptionResponse} from '../../core/models/getallsubscriptionresponse.models';

@Component({
  selector: 'app-view-subscription',
  standalone: true,
  imports: [ CommonModule],
  templateUrl: './view-subscription.html',
  styleUrl: './view-subscription.css'
})
export class ViewSubscriptionComponent implements OnInit {

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

      alert("Invalid Subscription Id");

      this.router.navigate(['/subscriptions']);

      return;

    }

    this.subscriptionService
      .getbyid(id)
      .subscribe({

        next: (res:createSubscriptionResponse) => {
          this.plan = res;
          this.loading = false;
        },

        error: (err:any) => 
          {
          console.log(err);
          alert("Unable to load subscription.");
          this.router.navigate(['/subscriptions']);
          }
      });

  }

  back(): void {

    this.router.navigate(['/subscriptions']);

  }

}