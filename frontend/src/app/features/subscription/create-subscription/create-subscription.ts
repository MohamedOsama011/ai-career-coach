import { Component ,signal} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { createSubscriptionResponse } from '../../../core/models/getallsubscriptionresponse.models';
import { SubscriptionServices } from '../../../core/services/subscription.services';


@Component({
  selector: 'app-create-subscription',
  imports: [FormsModule],
  templateUrl: './create-subscription.html',
  styleUrl: './create-subscription.css',
})
export class CreateSubscription {

constructor(
    private router: Router,
    private subscriptionService: SubscriptionServices) {}

plans:createSubscriptionResponse={
  name:"",
  price:0,
  Description:''
}

create(): void
{
  this.subscriptionService.createSubscription(this.plans).subscribe(
    {
      next:()=>{
                  console.log("created successfuly");
                  this.router.navigate(['/subscriptions']);
                },
      error:(err:any)=>
              {
                console.log("error");
                },
    }   );
}

cancel(): void {

    this.router.navigate(['/subscriptions']);

  }

}




