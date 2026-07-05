
 import { Component,OnInit,OnDestroy,signal } from '@angular/core';
  import { SubscriptionServices } from '../../../core/services/subscription.services';
  import{PaymentServices  } from '../../../core/services/payment.services'
  import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { SubscriptionPlan,getallsubscriptionresponse } from '../../../core/models/getallsubscriptionresponse.models';
import { Usersubscripions } from '../../../core/models/getallsubscriptionresponse.models';
import { UserPayment } from '../../../core/models/user-payment.model';
@Component({
  selector: 'app-usersubscription',
  imports: [],
  templateUrl: './usersubscription.html',
  styleUrl: './usersubscription.css',
})

export class Usersubscription implements OnInit  {

  
  constructor(private  Subscription:SubscriptionServices,private router: Router,private paymentServices:PaymentServices) {}
  
  userpayments=signal<UserPayment[]>([]);
  usersubscription=signal<Usersubscripions[]>([]);
  plans=signal< SubscriptionPlan[] >([]); 
  
    ngOnInit(): void {
      this.loadPlans();
      this.loaduserpayments();
      this.loadusersubscription();
    }
  
    loadPlans() {
      this.Subscription.getSubscriptions().subscribe({
        next: (response:getallsubscriptionresponse) => {
          this.plans .set( response.data);
          console.log(this.plans());
        },
        error: (err:any) => {
          console.error(err);
      }  });
  
    }
    

    loadusersubscription(){
    this.paymentServices.getallusersubscriptions().subscribe({
      next:(Response:Usersubscripions[])=>{
        this.usersubscription.set(Response);
      },
      error:(err)=>{
        console.error(err);
      }
    })
    }
    


    loaduserpayments()
    {
      this.paymentServices.getalluserpayments().subscribe({
        next:(Response:UserPayment[])=>{
          this.userpayments.set(Response);
        },
        error:(err)=>{
        console.error(err);
      }

      })
    }






    goToPaymentMethods(id:number):void{

  this.router.navigate(['/payment-methods',id]);

}
  }
  
  
  

