import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { SubscriptionServices } from '../../../core/services/subscription.services';

import {createSubscriptionResponse,SubscriptionPlan} from '../../../core/models/getallsubscriptionresponse.models';

@Component({
  selector: 'app-update-subscription',
  standalone: true,
  imports: [CommonModule,FormsModule],
  templateUrl: './update-subscription.html',
  styleUrl: './update-subscription.css'
})
export class UpdateSubscriptionComponent implements OnInit {

  subscriptionId!: string;

  newplan: createSubscriptionResponse = {
    name: '',
    price: 0,
    Description:''
  };
plan=signal< SubscriptionPlan>({name:'',price:0,subscriptions:[],id:0,Description:'',Createdatat:new Date}); 
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
        next: (response) => {
          console.log(response);
          console.log(response.data);
          this.plan .set( response.data);
          console.log(this.plan);
          this.newplan.name = this.plan().name;
          console.log(this.newplan.name);
          this.newplan.price = this.plan().price;
          console.log(this.newplan.price);
          this.newplan.Description=this.plan().Description;
          console.log(this.newplan.Description)
          this.loading = false;
        },

        error: (err) => {
          console.error(err);
          alert("Unable to load subscription.");
          this.router.navigate(['/subscriptions']);

        }

      });

  }

  update(): void {
  this.subscriptionService.updateSubscription(this.subscriptionId,this.newplan)
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