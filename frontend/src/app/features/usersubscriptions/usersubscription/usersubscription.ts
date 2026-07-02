
 import { Component,OnInit,OnDestroy,signal } from '@angular/core';
  import { SubscriptionServices } from '../../../core/services/subscription.services';
  import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterLink } from '@angular/router';
  import { SubscriptionPlan,updateSubscriptionResponse,getallsubscriptionresponse ,createSubscriptionResponse} from '../../../core/models/getallsubscriptionresponse.models';
@Component({
  selector: 'app-usersubscription',
  imports: [],
  templateUrl: './usersubscription.html',
  styleUrl: './usersubscription.css',
})

export class Usersubscription implements OnInit ,OnDestroy {

  
  constructor(private  Subscription:SubscriptionServices,private router: Router) {}
  
  // subscriptions:signal<getallsubscriptionresponse['data']> = signal([]);
  plans: SubscriptionPlan[] = []; 
  
    ngOnInit(): void {
      this.loadPlans();
    }
  
    loadPlans() {
      this.Subscription.getSubscriptions().subscribe({
        next: (response:getallsubscriptionresponse) => {
          this.plans = response.data;
          console.log(this.plans);
        },
        error: (err:any) => {
          console.error(err);
      }  });
  
    }
    goToPaymentMethods(id:number):void{

  this.router.navigate(['/payment-methods',id]);

}
  }
  
  
  

